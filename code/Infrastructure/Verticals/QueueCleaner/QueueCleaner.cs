using Common.Configuration.Arr;
using Common.Configuration.DownloadClient;
using Common.Configuration.QueueCleaner;
using Domain.Enums;
using Domain.Models.Arr;
using Domain.Models.Arr.Queue;
using Infrastructure.Providers;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.Arr.Interfaces;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.Jobs;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace Infrastructure.Verticals.QueueCleaner;

public sealed class QueueCleaner : GenericHandler
{
    private readonly QueueCleanerConfig _config;
    private readonly IgnoredDownloadsProvider<QueueCleanerConfig> _ignoredDownloadsProvider;

    public QueueCleaner(
        ILogger<QueueCleaner> logger,
        IOptions<QueueCleanerConfig> config,
        IOptions<DownloadClientConfig> downloadClientConfig,
        IOptions<SonarrConfig> sonarrConfig,
        IOptions<RadarrConfig> radarrConfig,
        IOptions<LidarrConfig> lidarrConfig,
        SonarrClient sonarrClient,
        RadarrClient radarrClient,
        LidarrClient lidarrClient,
        ArrQueueIterator arrArrQueueIterator,
        DownloadServiceFactory downloadServiceFactory,
        INotificationPublisher notifier,
        IgnoredDownloadsProvider<QueueCleanerConfig> ignoredDownloadsProvider
    ) : base(
        logger, downloadClientConfig,
        sonarrConfig, radarrConfig, lidarrConfig,
        sonarrClient, radarrClient, lidarrClient,
        arrArrQueueIterator, downloadServiceFactory,
        notifier
    )
    {
        _config = config.Value;
        _ignoredDownloadsProvider = ignoredDownloadsProvider;
    }
    
    protected override async Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType)
    {
        IReadOnlyList<string> ignoredDownloads = await _ignoredDownloadsProvider.GetIgnoredDownloads();
        
        using var _ = LogContext.PushProperty("InstanceName", instanceType.ToString());
        
        HashSet<SearchItem> itemsToBeRefreshed = [];
        IArrClient arrClient = GetClient(instanceType);
        
        // push to context
        ContextProvider.Set(nameof(ArrInstance) + nameof(ArrInstance.Url), instance.Url);
        ContextProvider.Set(nameof(InstanceType), instanceType);

        await _arrArrQueueIterator.Iterate(arrClient, instance, async items =>
        {
            var groups = items
                .GroupBy(x => x.DownloadId)
                .ToList();
            
            foreach (var group in groups)
            {
                if (group.Any(x => !arrClient.IsRecordValid(x)))
                {
                    continue;
                }
                
                QueueRecord record = group.First();
                
                if (!arrClient.IsRecordValid(record))
                {
                    continue;
                }
                
                if (ignoredDownloads.Contains(record.DownloadId, StringComparer.InvariantCultureIgnoreCase))
                {
                    _logger.LogInformation("skip | {title} | ignored", record.Title);
                    continue;
                }
                
                // push record to context
                ContextProvider.Set(nameof(QueueRecord), record);

                StalledResult stalledCheckResult = new();

                if (record.Protocol is "torrent")
                {
                    if (_downloadClientConfig.DownloadClient is Common.Enums.DownloadClient.None)
                    {
                        _logger.LogWarning("skip | download client is not configured | {title}", record.Title);
                        continue;
                    }
                    
                    // stalled download check
                    stalledCheckResult = await _downloadService.ShouldRemoveFromArrQueueAsync(record.DownloadId, ignoredDownloads);
                }
                
                // failed import check
                bool shouldRemoveFromArr = await arrClient.ShouldRemoveFromQueue(instanceType, record, stalledCheckResult.IsPrivate);
                DeleteReason deleteReason = stalledCheckResult.ShouldRemove ? stalledCheckResult.DeleteReason : DeleteReason.ImportFailed;

                if (!shouldRemoveFromArr && !stalledCheckResult.ShouldRemove)
                {
                    _logger.LogInformation("skip | {title}", record.Title);
                    continue;
                }
                
                itemsToBeRefreshed.Add(GetRecordSearchItem(instanceType, record, group.Count() > 1));

                bool removeFromClient = true;

                if (stalledCheckResult.IsPrivate)
                {
                    if (stalledCheckResult.ShouldRemove && !_config.StalledDeletePrivate)
                    {
                        removeFromClient = false;
                    }

                    if (shouldRemoveFromArr && !_config.ImportFailedDeletePrivate)
                    {
                        removeFromClient = false;
                    }
                }
                
                await arrClient.DeleteQueueItemAsync(instance, record, removeFromClient);
                await _notifier.NotifyQueueItemDeleted(removeFromClient, deleteReason);
            }
        });
        
        await arrClient.RefreshItemsAsync(instance, itemsToBeRefreshed);
    }
}