using Common.Configuration;
using Data.Models.Configuration.Arr;
using Data.Models.Configuration.General;
using Data.Models.Configuration.QueueCleaner;
using Common.Enums;
using Data;
using Data.Enums;
using Data.Models.Arr.Queue;
using Infrastructure.Events;
using Infrastructure.Helpers;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.Arr.Interfaces;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.Jobs;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LogContext = Serilog.Context.LogContext;
using Infrastructure.Verticals.ContentBlocker;

namespace Infrastructure.Verticals.QueueCleaner;

public sealed class QueueCleaner : GenericHandler
{
    private readonly BlocklistProvider _blocklistProvider;

    public QueueCleaner(
        ILogger<QueueCleaner> logger,
        DataContext dataContext,
        IMemoryCache cache,
        IBus messageBus,
        ArrClientFactory arrClientFactory,
        ArrQueueIterator arrArrQueueIterator,
        DownloadServiceFactory downloadServiceFactory,
        BlocklistProvider blocklistProvider,
        EventPublisher eventPublisher
    ) : base(
        logger, dataContext, cache, messageBus,
        arrClientFactory, arrArrQueueIterator, downloadServiceFactory, eventPublisher
    )
    {
        _blocklistProvider = blocklistProvider;
    }
    
    protected override async Task ExecuteInternalAsync()
    {
        if (ContextProvider.Get<List<DownloadClientConfig>>(nameof(DownloadClientConfig)).Count is 0)
        {
            _logger.LogWarning("No download clients configured");
            return;
        }
        
        await _blocklistProvider.LoadBlocklistsAsync();
        
        var sonarrConfig = ContextProvider.Get<ArrConfig>(nameof(InstanceType.Sonarr));
        var radarrConfig = ContextProvider.Get<ArrConfig>(nameof(InstanceType.Radarr));
        var lidarrConfig = ContextProvider.Get<ArrConfig>(nameof(InstanceType.Lidarr));
        
        await ProcessArrConfigAsync(sonarrConfig, InstanceType.Sonarr);
        await ProcessArrConfigAsync(radarrConfig, InstanceType.Radarr);
        await ProcessArrConfigAsync(lidarrConfig, InstanceType.Lidarr);
    }

    protected override async Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType, ArrConfig arrConfig)
    {
        IReadOnlyList<string> ignoredDownloads = ContextProvider.Get<GeneralConfig>().IgnoredDownloads;
        
        using var _ = LogContext.PushProperty(LogProperties.Category, instanceType.ToString());
        
        IArrClient arrClient = _arrClientFactory.GetClient(instanceType);
        
        // push to context
        ContextProvider.Set(nameof(ArrInstance) + nameof(ArrInstance.Url), instance.Url);
        ContextProvider.Set(nameof(InstanceType), instanceType);

        IReadOnlyList<IDownloadService> downloadServices = await GetInitializedDownloadServicesAsync();

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
                
                _logger.LogTrace("processing | {title} | {id}", record.Title, record.DownloadId);
                
                if (!arrClient.IsRecordValid(record))
                {
                    continue;
                }
                
                if (ignoredDownloads.Contains(record.DownloadId, StringComparer.InvariantCultureIgnoreCase))
                {
                    _logger.LogInformation("skip | {title} | ignored", record.Title);
                    continue;
                }
                
                string downloadRemovalKey = CacheKeys.DownloadMarkedForRemoval(record.DownloadId, instance.Url);
                
                if (_cache.TryGetValue(downloadRemovalKey, out bool _))
                {
                    _logger.LogDebug("skip | already marked for removal | {title}", record.Title);
                    continue;
                }
                
                // push record to context
                ContextProvider.Set(nameof(QueueRecord), record);

                DownloadCheckResult downloadCheckResult = new();

                if (record.Protocol is "torrent")
                {
                    var torrentClients = downloadServices
                        .Where(x => x.ClientConfig.Type is DownloadClientType.Torrent)
                        .ToList();
                    
                    if (torrentClients.Count > 0)
                    {
                        // Check each download client for the download item
                        foreach (var downloadService in downloadServices)
                        {
                            try
                            {
                                // stalled download check
                                DownloadCheckResult result = await downloadService
                                    .ShouldRemoveFromArrQueueAsync(record.DownloadId, ignoredDownloads);
                                
                                if (result.Found)
                                {
                                    downloadCheckResult = result;
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error checking download {dName} with download client {cName}", 
                                    record.Title, downloadService.ClientConfig.Name);
                            }
                        }
                    
                        if (!downloadCheckResult.Found)
                        {
                            _logger.LogWarning("Download not found in any torrent client | {title}", record.Title);
                        }
                    }
                }
                
                var config = ContextProvider.Get<QueueCleanerConfig>();
                
                // failed import check
                bool shouldRemoveFromArr = await arrClient.ShouldRemoveFromQueue(instanceType, record, downloadCheckResult.IsPrivate, config.FailedImport.MaxStrikes);
                DeleteReason deleteReason = downloadCheckResult.ShouldRemove ? downloadCheckResult.DeleteReason : DeleteReason.FailedImport;
                
                if (!shouldRemoveFromArr && !downloadCheckResult.ShouldRemove)
                {
                    _logger.LogInformation("skip | {title}", record.Title);
                    continue;
                }

                bool removeFromClient = true;
                
                if (downloadCheckResult.IsPrivate)
                {
                    bool isStalledWithoutPruneFlag = 
                        downloadCheckResult.DeleteReason is DeleteReason.Stalled &&
                        !config.Stalled.DeletePrivate;
    
                    bool isSlowWithoutPruneFlag = 
                        downloadCheckResult.DeleteReason is DeleteReason.SlowSpeed or DeleteReason.SlowTime &&
                        !config.Slow.DeletePrivate;
                        
                    bool isContentBlockerWithoutPruneFlag =
                        deleteReason is DeleteReason.AllFilesBlocked &&
                        !config.ContentBlocker.DeletePrivate;
    
                    bool shouldKeepDueToDeleteRules = downloadCheckResult.ShouldRemove && 
                        (isStalledWithoutPruneFlag || isSlowWithoutPruneFlag || isContentBlockerWithoutPruneFlag);
                        
                    bool shouldKeepDueToImportRules = shouldRemoveFromArr && !config.FailedImport.DeletePrivate;

                    if (shouldKeepDueToDeleteRules || shouldKeepDueToImportRules)
                    {
                        removeFromClient = false;
                    }
                }
                
                await PublishQueueItemRemoveRequest(
                    downloadRemovalKey,
                    instanceType,
                    instance,
                    record,
                    group.Count() > 1,
                    removeFromClient,
                    deleteReason
                );
            }
        });
    }
}