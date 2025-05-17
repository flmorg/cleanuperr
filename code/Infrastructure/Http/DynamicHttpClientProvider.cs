using System.Net;
using Common.Configuration.DownloadClient;
using Common.Configuration.General;
using Infrastructure.Configuration;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace Infrastructure.Http;

/// <summary>
/// Provides dynamically configured HTTP clients for download services
/// </summary>
public class DynamicHttpClientProvider : IDynamicHttpClientProvider
{
    private readonly ILogger<DynamicHttpClientProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfigManager _configManager;
    private readonly CertificateValidationService _certificateValidationService;

    public DynamicHttpClientProvider(
        ILogger<DynamicHttpClientProvider> logger,
        IHttpClientFactory httpClientFactory,
        IConfigManager configManager,
        CertificateValidationService certificateValidationService)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configManager = configManager;
        _certificateValidationService = certificateValidationService;
    }

    /// <inheritdoc />
    public HttpClient CreateClient(ClientConfig clientConfig)
    {
        if (clientConfig == null)
        {
            throw new ArgumentNullException(nameof(clientConfig));
        }
        
        // Try to use named client if it exists
        try
        {
            string clientName = GetClientName(clientConfig);
            return _httpClientFactory.CreateClient(clientName);
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning("Named HTTP client for {clientId} not found, creating generic client", clientConfig.Id);
            return CreateGenericClient(clientConfig);
        }
    }

    /// <summary>
    /// Gets the client name for a specific client configuration
    /// </summary>
    /// <param name="clientConfig">The client configuration</param>
    /// <returns>The client name for use with IHttpClientFactory</returns>
    private string GetClientName(ClientConfig clientConfig)
    {
        return $"DownloadClient_{clientConfig.Id}";
    }

    /// <summary>
    /// Creates a generic HTTP client with appropriate configuration
    /// </summary>
    /// <param name="clientConfig">The client configuration</param>
    /// <returns>A configured HttpClient instance</returns>
    private HttpClient CreateGenericClient(ClientConfig clientConfig)
    {
        var httpConfig = _configManager.GetConfiguration<GeneralConfig>("http.json") ?? new GeneralConfig();
        
        // Create handler with certificate validation
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                _certificateValidationService.ShouldByPassValidationError(
                    httpConfig.CertificateValidation,
                    sender,
                    certificate,
                    chain,
                    sslPolicyErrors
                ),
            UseDefaultCredentials = false
        };

        if (clientConfig.Type == Common.Enums.DownloadClientType.Deluge)
        {
            handler.AllowAutoRedirect = true;
            handler.UseCookies = true;
            handler.CookieContainer = new CookieContainer();
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }
        
        // Create client with policy
        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(httpConfig.HttpTimeout)
        };
        
        // Set base address if needed
        if (clientConfig.Url != null)
        {
            client.BaseAddress = clientConfig.Url;
        }
        
        _logger.LogDebug("Created generic HTTP client for client {clientId} with base address {baseAddress}", 
            clientConfig.Id, client.BaseAddress);
        
        return client;
    }
    
    /// <summary>
    /// Creates a retry policy for the HTTP client
    /// </summary>
    /// <param name="generalConfig">The HTTP configuration</param>
    /// <returns>A configured policy</returns>
    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(GeneralConfig generalConfig)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            // Do not retry on Unauthorized
            .OrResult(response => !response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Unauthorized)
            .WaitAndRetryAsync(generalConfig.HttpMaxRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}
