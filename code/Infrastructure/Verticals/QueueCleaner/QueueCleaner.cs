using Common.Configuration.Arr;
using Domain.Arr.Queue;
using Domain.Enums;
using Domain.Models.Arr;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.QueueCleaner;

public sealed class QueueCleaner : GenericHandler
{
    public QueueCleaner(
        ILogger<QueueCleaner> logger,
        IOptions<SonarrConfig> sonarrConfig,
        IOptions<RadarrConfig> radarrConfig,
        SonarrClient sonarrClient,
        RadarrClient radarrClient,
        ArrQueueIterator arrArrQueueIterator,
        DownloadServiceFactory downloadServiceFactory
    ) : base(logger, sonarrConfig.Value, radarrConfig.Value, sonarrClient, radarrClient, arrArrQueueIterator, downloadServiceFactory)
    {
    }
    
    protected override async Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType)
    {
        HashSet<SearchItem> itemsToBeRefreshed = [];
        ArrClient arrClient = GetClient(instanceType);
        ArrConfig arrConfig = GetConfig(instanceType);

        await _arrArrQueueIterator.Iterate(arrClient, instance, async items =>
        {
            foreach (QueueRecord record in items)
            {
                if (record.Protocol is not "torrent")
                {
                    continue;
                }

                if (string.IsNullOrEmpty(record.DownloadId))
                {
                    _logger.LogDebug("skip | download id is null for {title}", record.Title);
                    continue;
                }

                if (record.DownloadId.Equals(record.Title, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogDebug("skip | item was added a few seconds ago");
                    continue;
                }

                if (!await _downloadService.ShouldRemoveFromArrQueueAsync(record.DownloadId, arrConfig.StalledMaxStrikes))
                {
                    _logger.LogInformation("skip | {title}", record.Title);
                    continue;
                }

                itemsToBeRefreshed.Add(GetRecordSearchItem(instanceType, record));

                await arrClient.DeleteQueueItemAsync(instance, record);
            }
        });
        
        await arrClient.RefreshItemsAsync(instance, arrConfig, itemsToBeRefreshed);
    }
}