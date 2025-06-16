using System.Net;
using Common.Enums;

namespace Infrastructure.Http.DynamicHttpClientSystem;

/// <summary>
/// Implementation of the dynamic HttpClient factory
/// </summary>
public class DynamicHttpClientFactory : IDynamicHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpClientConfigStore _configStore;

    public DynamicHttpClientFactory(IHttpClientFactory httpClientFactory, IHttpClientConfigStore configStore)
    {
        _httpClientFactory = httpClientFactory;
        _configStore = configStore;
    }

    public HttpClient CreateClient(string clientName, HttpClientConfig config)
    {
        _configStore.AddConfiguration(clientName, config);
        return _httpClientFactory.CreateClient(clientName);
    }

    public HttpClient CreateClient(string clientName)
    {
        if (!_configStore.TryGetConfiguration(clientName, out _))
        {
            throw new InvalidOperationException($"No configuration found for client '{clientName}'. Register configuration first.");
        }
        
        return _httpClientFactory.CreateClient(clientName);
    }

    public void RegisterConfiguration(string clientName, HttpClientConfig config)
    {
        _configStore.AddConfiguration(clientName, config);
    }

    public void RegisterRetryClient(string clientName, int timeout, RetryConfig retryConfig, CertificateValidationType certificateType)
    {
        var config = new HttpClientConfig
        {
            Name = clientName,
            Timeout = timeout,
            Type = HttpClientType.WithRetry,
            RetryConfig = retryConfig,
            CertificateValidationType = certificateType
        };
        
        RegisterConfiguration(clientName, config);
    }

    public void RegisterDelugeClient(string clientName, int timeout, RetryConfig retryConfig, CertificateValidationType certificateType)
    {
        var config = new HttpClientConfig
        {
            Name = clientName,
            Timeout = timeout,
            Type = HttpClientType.Deluge,
            RetryConfig = retryConfig,
            AllowAutoRedirect = true,
            CertificateValidationType = certificateType,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        
        RegisterConfiguration(clientName, config);
    }

    public void RegisterDownloadClient(string clientName, int timeout, HttpClientType clientType, RetryConfig retryConfig, CertificateValidationType certificateType)
    {
        var config = new HttpClientConfig
        {
            Name = clientName,
            Timeout = timeout,
            Type = clientType,
            RetryConfig = retryConfig,
            CertificateValidationType = certificateType
        };

        // Configure Deluge-specific settings if needed
        if (clientType == HttpClientType.Deluge)
        {
            config.AllowAutoRedirect = true;
            config.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }
        
        RegisterConfiguration(clientName, config);
    }

    public void UnregisterConfiguration(string clientName)
    {
        _configStore.RemoveConfiguration(clientName);
    }
} 