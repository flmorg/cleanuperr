using System.Net;
using Common.Configuration;
using Common.Configuration.General;
using Data;
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
    private readonly DataContext _dataContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CertificateValidationService _certificateValidationService;

    public DynamicHttpClientProvider(
        ILogger<DynamicHttpClientProvider> logger,
        DataContext dataContext,
        IHttpClientFactory httpClientFactory,
        CertificateValidationService certificateValidationService)
    {
        _logger = logger;
        _dataContext = dataContext;
        _httpClientFactory = httpClientFactory;
        _certificateValidationService = certificateValidationService;
    }

    /// <inheritdoc />
    public HttpClient CreateClient(DownloadClientConfig downloadClientConfig)
    {
        if (downloadClientConfig == null)
        {
            throw new ArgumentNullException(nameof(downloadClientConfig));
        }
        
        // Try to use named client if it exists
        try
        {
            string clientName = GetClientName(downloadClientConfig);
            return _httpClientFactory.CreateClient(clientName);
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning("Named HTTP client for {clientId} not found, creating generic client", downloadClientConfig.Id);
            return CreateGenericClient(downloadClientConfig);
        }
    }

    /// <summary>
    /// Gets the client name for a specific client configuration
    /// </summary>
    /// <param name="downloadClientConfig">The client configuration</param>
    /// <returns>The client name for use with IHttpClientFactory</returns>
    private string GetClientName(DownloadClientConfig downloadClientConfig)
    {
        return $"DownloadClient_{downloadClientConfig.Id}";
    }

    /// <summary>
    /// Creates a generic HTTP client with appropriate configuration
    /// </summary>
    /// <param name="downloadClientConfig">The client configuration</param>
    /// <returns>A configured HttpClient instance</returns>
    private HttpClient CreateGenericClient(DownloadClientConfig downloadClientConfig)
    {
        // TODO
        var httpConfig = _dataContext.GeneralConfigs.First();
        
        // Create handler with certificate validation
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                _certificateValidationService.ShouldByPassValidationError(
                    httpConfig.HttpCertificateValidation,
                    sender,
                    certificate,
                    chain,
                    sslPolicyErrors
                ),
            UseDefaultCredentials = false
        };

        if (downloadClientConfig.TypeName == Common.Enums.DownloadClientTypeName.Deluge)
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
        if (downloadClientConfig.Url != null)
        {
            client.BaseAddress = downloadClientConfig.Url;
        }
        
        _logger.LogDebug("Created generic HTTP client for client {clientId} with base address {baseAddress}", 
            downloadClientConfig.Id, client.BaseAddress);
        
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
