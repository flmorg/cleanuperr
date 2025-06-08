using Common.Configuration.Arr;
using Common.Configuration.DownloadClient;
using Common.Configuration.General;
using Data.Enums;
using Data.Models.Arr;
using Data.Models.Arr.Queue;
using Infrastructure.Configuration;
using Infrastructure.Events;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.DownloadRemover.Models;
using Infrastructure.Verticals.Notifications;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.Jobs;

public abstract class GenericHandler : IHandler, IDisposable
{
    protected readonly ILogger<GenericHandler> _logger;
    protected readonly GeneralConfig _generalConfig;
    protected readonly DownloadClientConfig _downloadClientConfig;
    protected readonly SonarrConfig _sonarrConfig;
    protected readonly RadarrConfig _radarrConfig;
    protected readonly LidarrConfig _lidarrConfig;
    protected readonly IMemoryCache _cache;
    protected readonly IBus _messageBus;
    protected readonly ArrClientFactory _arrClientFactory;
    protected readonly ArrQueueIterator _arrArrQueueIterator;
    protected readonly DownloadServiceFactory _downloadServiceFactory;
    private readonly EventPublisher _eventPublisher;
    
    // Collection of download services for use with multiple clients
    protected readonly List<IDownloadService> _downloadServices = [];

    protected GenericHandler(
        ILogger<GenericHandler> logger,
        IMemoryCache cache,
        IBus messageBus,
        ArrClientFactory arrClientFactory,
        ArrQueueIterator arrArrQueueIterator,
        DownloadServiceFactory downloadServiceFactory,
        IConfigManager configManager,
        EventPublisher eventPublisher
    )
    {
        _logger = logger;
        _cache = cache;
        _messageBus = messageBus;
        _arrClientFactory = arrClientFactory;
        _arrArrQueueIterator = arrArrQueueIterator;
        _downloadServiceFactory = downloadServiceFactory;
        _eventPublisher = eventPublisher;
        _generalConfig = configManager.GetConfiguration<GeneralConfig>();
        _downloadClientConfig = configManager.GetConfiguration<DownloadClientConfig>();
        _sonarrConfig = configManager.GetConfiguration<SonarrConfig>();
        _radarrConfig = configManager.GetConfiguration<RadarrConfig>();
        _lidarrConfig = configManager.GetConfiguration<LidarrConfig>();
    }

    /// <summary>
    /// Initialize download services based on configuration
    /// </summary>
    protected virtual void InitializeDownloadServices()
    {
        // Clear any existing services
        DisposeDownloadServices();
        _downloadServices.Clear();
        
        // Add all enabled clients
        foreach (var client in _downloadClientConfig.GetEnabledClients())
        {
            try
            {
                var service = _downloadServiceFactory.GetDownloadService(client);
                if (service != null)
                {
                    _downloadServices.Add(service);
                    _logger.LogDebug("Initialized download client: {name}", client.Name);
                }
                else
                {
                    _logger.LogWarning("Download client service not available for: {name}", client.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize download client: {name}", client.Name);
            }
        }
        
        if (_downloadServices.Count == 0)
        {
            _logger.LogWarning("No enabled download clients found");
        }
        else
        {
            _logger.LogDebug("Initialized {count} download clients", _downloadServices.Count);
        }
    }

    public virtual async Task ExecuteAsync()
    {
        // Initialize download services
        InitializeDownloadServices();
        
        if (_downloadServices.Count == 0)
        {
            _logger.LogWarning("No download clients available, skipping execution");
            return;
        }
        
        // Login to all download services
        foreach (var downloadService in _downloadServices)
        {
            await downloadService.LoginAsync();
        }

        await ProcessArrConfigAsync(_sonarrConfig, InstanceType.Sonarr);
        await ProcessArrConfigAsync(_radarrConfig, InstanceType.Radarr);
        await ProcessArrConfigAsync(_lidarrConfig, InstanceType.Lidarr);
    }

    public virtual void Dispose()
    {
        DisposeDownloadServices();
    }
    
    /// <summary>
    /// Dispose all download services
    /// </summary>
    protected void DisposeDownloadServices()
    {
        foreach (var service in _downloadServices)
        {
            service.Dispose();
        }
    }

    protected abstract Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType, ArrConfig config);
    
    protected async Task ProcessArrConfigAsync(ArrConfig config, InstanceType instanceType, bool throwOnFailure = false)
    {
        if (!config.Enabled)
        {
            return;
        }

        foreach (ArrInstance arrInstance in config.Instances)
        {
            try
            {
                await ProcessInstanceAsync(arrInstance, instanceType, config);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "failed to process {type} instance | {url}", instanceType, arrInstance.Url);

                if (throwOnFailure)
                {
                    throw;
                }
            }
        }
    }

    protected async Task PublishQueueItemRemoveRequest(
        string downloadRemovalKey,
        InstanceType instanceType,
        ArrInstance instance,
        QueueRecord record,
        bool isPack,
        bool removeFromClient,
        DeleteReason deleteReason
    )
    {
        if (instanceType is InstanceType.Sonarr)
        {
            QueueItemRemoveRequest<SonarrSearchItem> removeRequest = new()
            {
                InstanceType = instanceType,
                Instance = instance,
                Record = record,
                SearchItem = (SonarrSearchItem)GetRecordSearchItem(instanceType, record, isPack),
                RemoveFromClient = removeFromClient,
                DeleteReason = deleteReason
            };

            await _messageBus.Publish(removeRequest);
        }
        else
        {
            QueueItemRemoveRequest<SearchItem> removeRequest = new()
            {
                InstanceType = instanceType,
                Instance = instance,
                Record = record,
                SearchItem = GetRecordSearchItem(instanceType, record, isPack),
                RemoveFromClient = removeFromClient,
                DeleteReason = deleteReason
            };

            await _messageBus.Publish(removeRequest);
        }

        _logger.LogInformation("item marked for removal | {title} | {url}", record.Title, instance.Url);
        await _eventPublisher.PublishAsync(EventType.DownloadMarkedForDeletion, "Download marked for deletion", EventSeverity.Important);
    }
    
    protected SearchItem GetRecordSearchItem(InstanceType type, QueueRecord record, bool isPack = false)
    {
        return type switch
        {
            InstanceType.Sonarr when _sonarrConfig.SearchType is SonarrSearchType.Episode && !isPack => new SonarrSearchItem
            {
                Id = record.EpisodeId,
                SeriesId = record.SeriesId,
                SearchType = SonarrSearchType.Episode
            },
            InstanceType.Sonarr when _sonarrConfig.SearchType is SonarrSearchType.Episode && isPack => new SonarrSearchItem
            {
                Id = record.SeasonNumber,
                SeriesId = record.SeriesId,
                SearchType = SonarrSearchType.Season
            },
            InstanceType.Sonarr when _sonarrConfig.SearchType is SonarrSearchType.Season => new SonarrSearchItem
            {
                Id = record.SeasonNumber,
                SeriesId = record.SeriesId,
                SearchType = SonarrSearchType.Series
            },
            InstanceType.Sonarr when _sonarrConfig.SearchType is SonarrSearchType.Series => new SonarrSearchItem
            {
                Id = record.SeriesId
            },
            InstanceType.Radarr => new SearchItem
            {
                Id = record.MovieId
            },
            InstanceType.Lidarr => new SearchItem
            {
                Id = record.AlbumId
            },
            _ => throw new NotImplementedException($"instance type {type} is not yet supported")
        };
    }
}