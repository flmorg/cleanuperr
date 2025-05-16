using System.Collections.Concurrent;
using Common.Configuration.DownloadClient;
using Common.Enums;
using Domain.Exceptions;
using Infrastructure.Configuration;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.DownloadClient.Deluge;
using Infrastructure.Verticals.DownloadClient.QBittorrent;
using Infrastructure.Verticals.DownloadClient.Transmission;
using Infrastructure.Verticals.Files;
using Infrastructure.Verticals.ItemStriker;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.DownloadClient.Factory;

/// <summary>
/// Factory for creating and managing download client service instances
/// </summary>
public class DownloadClientFactory : IDownloadClientFactory
{
    private readonly ILogger<DownloadClientFactory> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigManager _configManager;
    private readonly ConcurrentDictionary<string, IDownloadService> _clients = new();

    public DownloadClientFactory(
        ILogger<DownloadClientFactory> logger,
        IServiceProvider serviceProvider,
        IConfigManager configManager)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configManager = configManager;
    }

    /// <inheritdoc />
    public IDownloadService GetClient(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException("Client ID cannot be empty", nameof(clientId));
        }

        return _clients.GetOrAdd(clientId, CreateClient);
    }

    /// <inheritdoc />
    public IEnumerable<IDownloadService> GetAllEnabledClients()
    {
        var downloadClientConfig = _configManager.GetConfiguration<DownloadClientConfig>("downloadclients.json") 
                                  ?? new DownloadClientConfig();
        
        foreach (var client in downloadClientConfig.GetEnabledClients())
        {
            yield return GetClient(client.Id);
        }
    }

    /// <inheritdoc />
    public IEnumerable<IDownloadService> GetClientsByType(DownloadClient clientType)
    {
        var downloadClientConfig = _configManager.GetConfiguration<DownloadClientConfig>("downloadclients.json") 
                                  ?? new DownloadClientConfig();
        
        foreach (var client in downloadClientConfig.GetEnabledClients().Where(c => c.Type == clientType))
        {
            yield return GetClient(client.Id);
        }
    }

    /// <inheritdoc />
    public void RefreshClient(string clientId)
    {
        if (_clients.TryRemove(clientId, out var service))
        {
            service.Dispose();
            _logger.LogDebug("Removed client {clientId} from cache", clientId);
        }
        
        // Re-create and add the client
        _clients[clientId] = CreateClient(clientId);
        _logger.LogDebug("Re-created client {clientId}", clientId);
    }

    /// <inheritdoc />
    public void RefreshAllClients()
    {
        _logger.LogInformation("Refreshing all download clients");
        
        // Get list of client IDs to avoid modifying collection during iteration
        var clientIds = _clients.Keys.ToList();
        
        foreach (var clientId in clientIds)
        {
            RefreshClient(clientId);
        }
    }

    private IDownloadService CreateClient(string clientId)
    {
        var downloadClientConfig = _configManager.GetConfiguration<DownloadClientConfig>("downloadclients.json") 
                                  ?? new DownloadClientConfig();
        
        var clientConfig = downloadClientConfig.GetClientConfig(clientId);
        
        if (clientConfig == null)
        {
            throw new NotFoundException($"No configuration found for client with ID {clientId}");
        }

        IDownloadService service = clientConfig.Type switch
        {
            DownloadClient.QBittorrent => CreateQBitService(clientConfig),
            DownloadClient.Transmission => CreateTransmissionService(clientConfig),
            DownloadClient.Deluge => CreateDelugeService(clientConfig),
            _ => throw new NotSupportedException($"Download client type {clientConfig.Type} is not supported")
        };

        // Initialize the service with its configuration
        service.Initialize(clientConfig);
        
        _logger.LogInformation("Created client {clientName} ({clientId}) of type {clientType}", 
            clientConfig.Name, clientId, clientConfig.Type);
        
        return service;
    }

    private QBitService CreateQBitService(ClientConfig clientConfig)
    {
        return new QBitService(
            _serviceProvider.GetRequiredService<ILogger<QBitService>>(),
            _serviceProvider.GetRequiredService<IHttpClientFactory>(),
            _configManager,
            _serviceProvider.GetRequiredService<IMemoryCache>(),
            _serviceProvider.GetRequiredService<IFilenameEvaluator>(),
            _serviceProvider.GetRequiredService<IStriker>(),
            _serviceProvider.GetRequiredService<INotificationPublisher>(),
            _serviceProvider.GetRequiredService<IDryRunInterceptor>(),
            _serviceProvider.GetRequiredService<IHardLinkFileService>()
        );
    }

    private TransmissionService CreateTransmissionService(ClientConfig clientConfig)
    {
        return new TransmissionService(
            _serviceProvider.GetRequiredService<ILogger<TransmissionService>>(),
            _configManager,
            _serviceProvider.GetRequiredService<IMemoryCache>(),
            _serviceProvider.GetRequiredService<IFilenameEvaluator>(),
            _serviceProvider.GetRequiredService<IStriker>(),
            _serviceProvider.GetRequiredService<INotificationPublisher>(),
            _serviceProvider.GetRequiredService<IDryRunInterceptor>(),
            _serviceProvider.GetRequiredService<IHardLinkFileService>()
        );
    }

    private DelugeService CreateDelugeService(ClientConfig clientConfig)
    {
        return new DelugeService(
            _serviceProvider.GetRequiredService<ILogger<DelugeService>>(),
            _configManager,
            _serviceProvider.GetRequiredService<IMemoryCache>(),
            _serviceProvider.GetRequiredService<IFilenameEvaluator>(),
            _serviceProvider.GetRequiredService<IStriker>(),
            _serviceProvider.GetRequiredService<INotificationPublisher>(),
            _serviceProvider.GetRequiredService<IDryRunInterceptor>(),
            _serviceProvider.GetRequiredService<IHardLinkFileService>()
        );
    }
}
