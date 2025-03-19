using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.Arr;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadClient;
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

namespace Infrastructure.Verticals.ContentBlocker;

public sealed class ContentBlocker : GenericHandler
{
    private readonly ContentBlockerConfig _config;
    private readonly BlocklistProvider _blocklistProvider;
    private readonly IgnoredDownloadsProvider<ContentBlockerConfig> _ignoredDownloadsProvider;

    public ContentBlocker(
        ILogger<ContentBlocker> logger,
        IOptions<ContentBlockerConfig> config,
        IOptions<DownloadClientConfig> downloadClientConfig,
        IOptions<SonarrConfig> sonarrConfig,
        IOptions<RadarrConfig> radarrConfig,
        IOptions<LidarrConfig> lidarrConfig,
        SonarrClient sonarrClient,
        RadarrClient radarrClient,
        LidarrClient lidarrClient,
        ArrQueueIterator arrArrQueueIterator,
        BlocklistProvider blocklistProvider,
        DownloadServiceFactory downloadServiceFactory,
        INotificationPublisher notifier,
        IgnoredDownloadsProvider<ContentBlockerConfig> ignoredDownloadsProvider
    ) : base(
        logger, downloadClientConfig,
        sonarrConfig, radarrConfig, lidarrConfig,
        sonarrClient, radarrClient, lidarrClient,
        arrArrQueueIterator, downloadServiceFactory,
        notifier
    )
    {
        _config = config.Value;
        _blocklistProvider = blocklistProvider;
        _ignoredDownloadsProvider = ignoredDownloadsProvider;
    }

    public override async Task ExecuteAsync()
    {
        if (_downloadClientConfig.DownloadClient is Common.Enums.DownloadClient.None or Common.Enums.DownloadClient.Disabled)
        {
            _logger.LogWarning("download client is not set");
            return;
        }
        
        bool blocklistIsConfigured = _sonarrConfig.Enabled && !string.IsNullOrEmpty(_sonarrConfig.Block.Path) ||
                                     _radarrConfig.Enabled && !string.IsNullOrEmpty(_radarrConfig.Block.Path) ||
                                     _lidarrConfig.Enabled && !string.IsNullOrEmpty(_lidarrConfig.Block.Path);

        if (!blocklistIsConfigured)
        {
            _logger.LogWarning("no blocklist is configured");
            return;
        }
        
        await _blocklistProvider.LoadBlocklistsAsync();
        await base.ExecuteAsync();
    }

    protected override async Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType)
    {
        IReadOnlyList<string> ignoredDownloads = await _ignoredDownloadsProvider.GetIgnoredDownloads();
        
        using var _ = LogContext.PushProperty("InstanceName", instanceType.ToString());

        HashSet<SearchItem> itemsToBeRefreshed = [];
        IArrClient arrClient = GetClient(instanceType);
        BlocklistType blocklistType = _blocklistProvider.GetBlocklistType(instanceType);
        ConcurrentBag<string> patterns = _blocklistProvider.GetPatterns(instanceType);
        ConcurrentBag<Regex> regexes = _blocklistProvider.GetRegexes(instanceType);
        
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
                QueueRecord record = group.First();
                
                if (record.Protocol is not "torrent")
                {
                    continue;
                }
                
                if (string.IsNullOrEmpty(record.DownloadId))
                {
                    _logger.LogDebug("skip | download id is null for {title}", record.Title);
                    continue;
                }
                
                if (ignoredDownloads.Contains(record.DownloadId, StringComparer.InvariantCultureIgnoreCase))
                {
                    _logger.LogInformation("skip | {title} | ignored", record.Title);
                    continue;
                }
                
                // push record to context
                ContextProvider.Set(nameof(QueueRecord), record);
                
                _logger.LogDebug("searching unwanted files for {title}", record.Title);

                BlockFilesResult result = await _downloadService
                    .BlockUnwantedFilesAsync(record.DownloadId, blocklistType, patterns, regexes, ignoredDownloads);
                
                if (!result.ShouldRemove)
                {
                    continue;
                }
                
                _logger.LogDebug("all files are marked as unwanted | {hash}", record.Title);
                
                itemsToBeRefreshed.Add(GetRecordSearchItem(instanceType, record, group.Count() > 1));

                bool removeFromClient = true;
                
                if (result.IsPrivate && !_config.DeletePrivate)
                {
                    removeFromClient = false;
                }
                
                await arrClient.DeleteQueueItemAsync(instance, record, removeFromClient);
                await _notifier.NotifyQueueItemDeleted(removeFromClient, DeleteReason.AllFilesBlocked);
            }
        });
        
        await arrClient.RefreshItemsAsync(instance, itemsToBeRefreshed);
    }
}