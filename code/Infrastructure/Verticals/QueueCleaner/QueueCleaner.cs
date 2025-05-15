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
    private readonly QueueCleanerConfig _config;
    private readonly IMemoryCache _cache;
    private readonly IConfigManager _configManager;
    private readonly IIgnoredDownloadsService _ignoredDownloadsService;
    private readonly List<IDownloadService> _downloadServices;

    public QueueCleaner(
        ILogger<QueueCleaner> logger,
        IConfigManager configManager,
        IMemoryCache cache,
        IBus messageBus,
        ArrClientFactory arrClientFactory,
        ArrQueueIterator arrArrQueueIterator,
        DownloadServiceFactory downloadServiceFactory,
        INotificationPublisher notifier,
        IIgnoredDownloadsService ignoredDownloadsService
    ) : base(
        logger, cache, messageBus,
        arrClientFactory, arrArrQueueIterator, downloadServiceFactory,
        notifier
    )
    {
        _configManager = configManager;
        _cache = cache;
        _ignoredDownloadsService = ignoredDownloadsService;
        _downloadServices = new List<IDownloadService>();
        
        // Initialize the configuration
        var configTask = _configManager.GetQueueCleanerConfigAsync();
        configTask.Wait();
        _config = configTask.Result ?? new QueueCleanerConfig();
        if (_config != null)
        {
            _config.Validate();
        }
        
        // Initialize base class configs
        InitializeConfigs().Wait();
    }
    
    private async Task InitializeConfigs()
    {
        // Get configurations from the configuration manager
        _downloadClientConfig = await _configManager.GetDownloadClientConfigAsync() ?? new DownloadClientConfig();
        _sonarrConfig = await _configManager.GetSonarrConfigAsync() ?? new SonarrConfig();
        _radarrConfig = await _configManager.GetRadarrConfigAsync() ?? new RadarrConfig();
        _lidarrConfig = await _configManager.GetLidarrConfigAsync() ?? new LidarrConfig();
        
        // Initialize download services
        if (_downloadClientConfig.Clients.Count > 0)
        {
            foreach (var client in _downloadClientConfig.GetEnabledClients())
            {
                try
                {
                    var service = _downloadServiceFactory.GetDownloadService(client.Id);
                    if (service != null)
                    {
                        _downloadServices.Add(service);
                        _logger.LogDebug("Added download client: {name} ({id})", client.Name, client.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Download client service not available for: {id}", client.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error initializing download client {id}: {message}", client.Id, ex.Message);
                }
            }
        }
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
                    foreach (var downloadService in _downloadServices)
                    {
                        try
                        {
                            // stalled download check
                            var result = await downloadService.ShouldRemoveFromArrQueueAsync(record.DownloadId, ignoredDownloads);
                            if (result.Found)
                            {
                                downloadCheckResult = result;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error checking download {id} with download client", record.DownloadId);
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