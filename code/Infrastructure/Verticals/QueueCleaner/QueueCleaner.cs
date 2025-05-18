using Common.Configuration.Arr;
using Common.Configuration.DownloadClient;
using Common.Configuration.QueueCleaner;
using Domain.Enums;
using Domain.Models.Arr;
using Domain.Models.Arr.Queue;
using Infrastructure.Configuration;
using Infrastructure.Helpers;
using Infrastructure.Services;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.Arr.Interfaces;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.DownloadClient.Factory;
using Infrastructure.Verticals.DownloadRemover.Models;
using Infrastructure.Verticals.Jobs;
using Infrastructure.Verticals.Notifications;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LogContext = Serilog.Context.LogContext;
using System.Collections.Generic;

namespace Infrastructure.Verticals.QueueCleaner;

public sealed class QueueCleaner : GenericHandler
{
    private QueueCleanerConfig _config;
    private readonly IConfigManager _configManager;
    private readonly IIgnoredDownloadsService _ignoredDownloadsService;
    private readonly IDownloadClientFactory _downloadClientFactory;

    public QueueCleaner(
        ILogger<QueueCleaner> logger,
        IConfigManager configManager,
        IMemoryCache cache,
        IBus messageBus,
        ArrClientFactory arrClientFactory,
        ArrQueueIterator arrArrQueueIterator,
        DownloadServiceFactory downloadServiceFactory,
        INotificationPublisher notifier,
        IIgnoredDownloadsService ignoredDownloadsService,
        IDownloadClientFactory downloadClientFactory
    ) : base(
        logger, cache, messageBus,
        arrClientFactory, arrArrQueueIterator, downloadServiceFactory,
        notifier
    )
    {
        _configManager = configManager;
        _ignoredDownloadsService = ignoredDownloadsService;
        _downloadClientFactory = downloadClientFactory;
        
        _config = configManager.GetConfiguration<QueueCleanerConfig>();
        _downloadClientConfig = configManager.GetConfiguration<DownloadClientConfig>();
        _sonarrConfig = configManager.GetConfiguration<SonarrConfig>();
        _radarrConfig = configManager.GetConfiguration<RadarrConfig>();
        _lidarrConfig = configManager.GetConfiguration<LidarrConfig>();
    }
    
    protected override async Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType, ArrConfig config)
    {
        IReadOnlyList<string> ignoredDownloads = await _ignoredDownloadsService.GetIgnoredDownloadsAsync();
        
        using var _ = LogContext.PushProperty("InstanceName", instanceType.ToString());
        
        IArrClient arrClient = _arrClientFactory.GetClient(instanceType);
        
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
                    if (_downloadClientConfig.Clients.Count == 0)
                    {
                        _logger.LogWarning("skip | no download clients configured | {title}", record.Title);
                        continue;
                    }
                    
                    // Check each download client for the download item
                    foreach (var downloadService in _downloadClientFactory.GetAllEnabledClients())
                    {
                        try
                        {
                            // stalled download check
                            var result = await downloadService.ShouldRemoveFromArrQueueAsync(record.DownloadId, ignoredDownloads);
                            if (result.Found)
                            {
                                downloadCheckResult = result;
                                // Add client ID to context for tracking
                                ContextProvider.Set("ClientId", downloadService.GetClientId());
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error checking download {id} with download client {clientId}", 
                                record.DownloadId, downloadService.GetClientId());
                        }
                    }
                    
                    if (!downloadCheckResult.Found)
                    {
                        _logger.LogWarning("skip | download not found {title}", record.Title);
                    }
                }
                
                // failed import check
                bool shouldRemoveFromArr = await arrClient.ShouldRemoveFromQueue(instanceType, record, downloadCheckResult.IsPrivate, config.ImportFailedMaxStrikes);
                DeleteReason deleteReason = downloadCheckResult.ShouldRemove ? downloadCheckResult.DeleteReason : DeleteReason.ImportFailed;

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
                        !_config.StalledDeletePrivate;
    
                    bool isSlowWithoutPruneFlag = 
                        downloadCheckResult.DeleteReason is DeleteReason.SlowSpeed or DeleteReason.SlowTime &&
                        !_config.SlowDeletePrivate;
    
                    bool shouldKeepDueToDeleteRules = downloadCheckResult.ShouldRemove && (isStalledWithoutPruneFlag || isSlowWithoutPruneFlag);
                    bool shouldKeepDueToImportRules = shouldRemoveFromArr && !_config.ImportFailedDeletePrivate;

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