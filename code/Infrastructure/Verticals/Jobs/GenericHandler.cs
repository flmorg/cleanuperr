using Common.Configuration.Arr;
using Common.Configuration.DownloadClient;
using Domain.Enums;
using Domain.Models.Arr;
using Domain.Models.Arr.Queue;
using Infrastructure.Configuration;
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
    protected DownloadClientConfig _downloadClientConfig = new();
    protected SonarrConfig _sonarrConfig = new();
    protected RadarrConfig _radarrConfig = new();
    protected LidarrConfig _lidarrConfig = new();
    protected readonly IMemoryCache _cache;
    protected readonly IBus _messageBus;
    protected readonly ArrClientFactory _arrClientFactory;
    protected readonly ArrQueueIterator _arrArrQueueIterator;
    protected readonly IDownloadService _downloadService;
    protected readonly INotificationPublisher _notifier;

    protected GenericHandler(
        ILogger<GenericHandler> logger,
        IMemoryCache cache,
        IBus messageBus,
        ArrClientFactory arrClientFactory,
        ArrQueueIterator arrArrQueueIterator,
        DownloadServiceFactory downloadServiceFactory,
        INotificationPublisher notifier
    )
    {
        _logger = logger;
        _cache = cache;
        _messageBus = messageBus;
        _arrClientFactory = arrClientFactory;
        _arrArrQueueIterator = arrArrQueueIterator;
        _downloadService = downloadServiceFactory.CreateDownloadClient();
        _notifier = notifier;
    }

    public virtual async Task ExecuteAsync()
    {
        await _downloadService.LoginAsync();

        await ProcessArrConfigAsync(_sonarrConfig, InstanceType.Sonarr);
        await ProcessArrConfigAsync(_radarrConfig, InstanceType.Radarr);
        await ProcessArrConfigAsync(_lidarrConfig, InstanceType.Lidarr);
    }

    public virtual void Dispose()
    {
        _downloadService.Dispose();
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
                _logger.LogError(exception, "failed to clean {type} instance | {url}", instanceType, arrInstance.Url);

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
        
        _cache.Set(downloadRemovalKey, true);
        _logger.LogInformation("item marked for removal | {title} | {url}", record.Title, instance.Url);
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