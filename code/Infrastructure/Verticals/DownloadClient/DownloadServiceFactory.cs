using Common.Configuration.DownloadClient;
using Common.Enums;
using Infrastructure.Configuration;
using Infrastructure.Verticals.DownloadClient.Deluge;
using Infrastructure.Verticals.DownloadClient.QBittorrent;
using Infrastructure.Verticals.DownloadClient.Transmission;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.DownloadClient;

/// <summary>
/// Factory responsible for creating download client service instances
/// </summary>
public sealed class DownloadServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigManager _configManager;
    private readonly ILogger<DownloadServiceFactory> _logger;
    
    public DownloadServiceFactory(
        IServiceProvider serviceProvider, 
        IConfigManager configManager,
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
    /// <returns>An implementation of IDownloadService or null if the client is not available</returns>
    public IDownloadService? GetDownloadService(Guid clientId)
    {
        var config = _configManager.GetConfiguration<DownloadClientConfig>();
        var clientConfig = config.GetClientConfig(clientId);
        
        if (clientConfig == null)
        {
            _logger.LogWarning("No download client configuration found for ID {clientId}", clientId);
            return null;
        }
        
        if (!clientConfig.Enabled)
        {
            _logger.LogWarning("Download client {clientId} is disabled", clientId);
            return null;
        }
        
        return GetDownloadService(clientConfig);
    }
    
    /// <summary>
    /// Creates a download service using the specified client configuration
    /// </summary>
    /// <param name="clientConfig">The client configuration to use</param>
    /// <returns>An implementation of IDownloadService or null if the client is not available</returns>
    public IDownloadService? GetDownloadService(ClientConfig clientConfig)
    {
        if (!clientConfig.Enabled)
        {
            _logger.LogWarning("Download client {clientId} is disabled", clientConfig.Id);
            return null;
        }
        
        return clientConfig.Type switch
        {
            DownloadClientType.QBittorrent => CreateClientService<QBitService>(clientConfig),
            DownloadClientType.Deluge => CreateClientService<DelugeService>(clientConfig),
            DownloadClientType.Transmission => CreateClientService<TransmissionService>(clientConfig),
            _ => null
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
        // TODO
        var service = _serviceProvider.GetRequiredService<T>();
        service.Initialize(clientConfig);
        return service;
    }
}