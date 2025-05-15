using Common.Configuration.DownloadClient;
using Common.Enums;
using Infrastructure.Configuration;
using Infrastructure.Verticals.DownloadClient.Deluge;
using Infrastructure.Verticals.DownloadClient.QBittorrent;
using Infrastructure.Verticals.DownloadClient.Transmission;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.DownloadClient;

public sealed class DownloadServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigurationManager _configManager;
    private readonly ILogger<DownloadServiceFactory> _logger;
    
    public DownloadServiceFactory(
        IServiceProvider serviceProvider, 
        IConfigurationManager configManager,
        ILogger<DownloadServiceFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _configManager = configManager;
        _logger = logger;
    }

    /// <summary>
    /// Creates a download service using the specified client ID
    /// </summary>
    /// <param name="clientId">The client ID to create a service for</param>
    /// <returns>An implementation of IDownloadService</returns>
    public IDownloadService GetDownloadService(string clientId)
    {
        var config = _configManager.GetDownloadClientConfigAsync().GetAwaiter().GetResult();
        
        if (config == null)
        {
            _logger.LogWarning("No download client configuration found, using empty service");
            return _serviceProvider.GetRequiredService<EmptyDownloadService>();
        }
        
        var clientConfig = config.GetClientConfig(clientId);
        
        if (clientConfig == null)
        {
            _logger.LogWarning("No download client configuration found for ID {clientId}, using empty service", clientId);
            return _serviceProvider.GetRequiredService<EmptyDownloadService>();
        }
        
        if (!clientConfig.Enabled)
        {
            _logger.LogWarning("Download client {clientId} is disabled, using empty service", clientId);
            return _serviceProvider.GetRequiredService<EmptyDownloadService>();
        }
        
        return GetDownloadService(clientConfig);
    }
    
    /// <summary>
    /// Creates a download service using the specified client configuration
    /// </summary>
    /// <param name="clientConfig">The client configuration to use</param>
    /// <returns>An implementation of IDownloadService</returns>
    public IDownloadService GetDownloadService(ClientConfig clientConfig)
    {
        if (clientConfig == null)
        {
            _logger.LogWarning("Client configuration is null, using empty service");
            return _serviceProvider.GetRequiredService<EmptyDownloadService>();
        }
        
        if (!clientConfig.Enabled)
        {
            _logger.LogWarning("Download client {clientId} is disabled, using empty service", clientConfig.Id);
            return _serviceProvider.GetRequiredService<EmptyDownloadService>();
        }
        
        return clientConfig.Type switch
        {
            DownloadClientType.QBittorrent => CreateClientService<QBitService>(clientConfig),
            DownloadClientType.Deluge => CreateClientService<DelugeService>(clientConfig),
            DownloadClientType.Transmission => CreateClientService<TransmissionService>(clientConfig),
            DownloadClientType.Usenet => _serviceProvider.GetRequiredService<EmptyDownloadService>(),
            DownloadClientType.Disabled => _serviceProvider.GetRequiredService<EmptyDownloadService>(),
            _ => _serviceProvider.GetRequiredService<EmptyDownloadService>()
        };
    }
    
    /// <summary>
    /// Creates a download client service for a specific client type
    /// </summary>
    /// <typeparam name="T">The type of download service to create</typeparam>
    /// <param name="clientConfig">The client configuration</param>
    /// <returns>An implementation of IDownloadService</returns>
    private T CreateClientService<T>(ClientConfig clientConfig) where T : IDownloadService
    {
        var service = _serviceProvider.GetRequiredService<T>();
        service.Initialize(clientConfig);
        return service;
    }
}