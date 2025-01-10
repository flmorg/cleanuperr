using Common.Configuration.Arr;
using Common.Configuration.DownloadClient;
using Domain.Enums;
using Domain.Models.Arr;
using Domain.Models.Arr.Queue;
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
        IOptions<DownloadClientConfig> downloadClientConfig,
        IOptions<SonarrConfig> sonarrConfig,
        IOptions<RadarrConfig> radarrConfig,
        SonarrClient sonarrClient,
        RadarrClient radarrClient,
        ArrQueueIterator arrArrQueueIterator,
        DownloadServiceFactory downloadServiceFactory
    ) : base(logger, downloadClientConfig, sonarrConfig.Value, radarrConfig.Value, sonarrClient, radarrClient, arrArrQueueIterator, downloadServiceFactory)
    {
    }
    
    protected override async Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType)
    {
        HashSet<SearchItem> itemsToBeRefreshed = [];
        ArrClient arrClient = GetClient(instanceType);
        ArrConfig arrConfig = GetConfig(instanceType);

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
                
                if (record.Protocol is not "torrent")
                {
                    continue;
                }

                if (!arrClient.IsRecordValid(record))
                {
                    continue;
                }

                bool shouldRemoveFromArr = arrClient.ShouldRemoveFromQueue(record);
                bool shouldRemoveFromDownloadClient = _downloadClientConfig.DownloadClient is not Common.Enums.DownloadClient.None &&
                    await _downloadService.ShouldRemoveFromArrQueueAsync(record.DownloadId);

                if (!shouldRemoveFromArr && !shouldRemoveFromDownloadClient)
                {
                    _logger.LogInformation("skip | {title}", record.Title);
                    continue;
                }
                
                itemsToBeRefreshed.Add(GetRecordSearchItem(instanceType, record, group.Count() > 1));

                await arrClient.DeleteQueueItemAsync(instance, record);
            }
        });
        
        await arrClient.RefreshItemsAsync(instance, arrConfig, itemsToBeRefreshed);
    }
}