using Common.Configuration.Arr;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Domain.Enums;
using Domain.Models.Arr.Queue;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.Arr.Interfaces;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.Jobs;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace Infrastructure.Verticals.DownloadCleaner;

public sealed class DownloadCleaner : GenericHandler
{
    private readonly DownloadCleanerConfig _config;
    private readonly HashSet<string> _excludedHashes = [];
    
    private static bool _hardLinkCategoryCreated;
    
    public DownloadCleaner(
        ILogger<DownloadCleaner> logger,
        IOptions<DownloadCleanerConfig> config,
        IOptions<DownloadClientConfig> downloadClientConfig,
        IOptions<SonarrConfig> sonarrConfig,
        IOptions<RadarrConfig> radarrConfig,
        IOptions<LidarrConfig> lidarrConfig,
        SonarrClient sonarrClient,
        RadarrClient radarrClient,
        LidarrClient lidarrClient,
        ArrQueueIterator arrArrQueueIterator,
        DownloadServiceFactory downloadServiceFactory,
        INotificationPublisher notifier
    ) : base(
        logger, downloadClientConfig,
        sonarrConfig, radarrConfig, lidarrConfig,
        sonarrClient, radarrClient, lidarrClient,
        arrArrQueueIterator, downloadServiceFactory,
        notifier
    )
    {
        _config = config.Value;
        _config.Validate();
    }
    
    public override async Task ExecuteAsync()
    {
        if (_downloadClientConfig.DownloadClient is Common.Enums.DownloadClient.None)
        {
            _logger.LogWarning("download client is set to none");
            return;
        }
        
        if (_config.Categories?.Count is null or 0)
        {
            _logger.LogWarning("no categories configured");
            return;
        }
        
        await _downloadService.LoginAsync();
        List<object>? downloads = await _downloadService.GetSeedingDownloads();
        List<object>? downloadsToChangeCategory = null;
        
        if (!string.IsNullOrEmpty(_config.NoHardLinksCategory) && _config.NoHardLinksCategories?.Count > 0)
        {
            if (!_hardLinkCategoryCreated)
            {
                _logger.LogDebug("creating category {cat}", _config.NoHardLinksCategory);

                await _downloadService.CreateCategoryAsync(_config.NoHardLinksCategory);
                _hardLinkCategoryCreated = true;
            }
            
            downloadsToChangeCategory = _downloadService.FilterDownloadsToChangeCategoryAsync(downloads, _config.NoHardLinksCategories);
        }

        // wait for the downloads to appear in the arr queue
        await Task.Delay(10 * 1000);

        await ProcessArrConfigAsync(_sonarrConfig, InstanceType.Sonarr, true);
        await ProcessArrConfigAsync(_radarrConfig, InstanceType.Radarr, true);
        await ProcessArrConfigAsync(_lidarrConfig, InstanceType.Lidarr, true);
        
        _logger.LogTrace("looking for downloads to change category");
        await _downloadService.ChangeCategoryForNoHardLinksAsync(downloadsToChangeCategory, _excludedHashes);
        
        List<object>? downloadsToClean = _downloadService.FilterDownloadsToBeCleanedAsync(downloads, _config.Categories);
        
        // release unused objects
        downloads = null;
        
        _logger.LogTrace("looking for downloads to clean");
        await _downloadService.CleanDownloadsAsync(downloadsToClean, _config.Categories, _excludedHashes);
    }

    protected override async Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType)
    {
        using var _ = LogContext.PushProperty("InstanceName", instanceType.ToString());
        
        IArrClient arrClient = GetClient(instanceType);
        
        await _arrArrQueueIterator.Iterate(arrClient, instance, async items =>
        {
            var groups = items
                .Where(x => !string.IsNullOrEmpty(x.DownloadId))
                .GroupBy(x => x.DownloadId)
                .ToList();

            foreach (QueueRecord record in groups.Select(group => group.First()))
            {
                _excludedHashes.Add(record.DownloadId.ToLowerInvariant());
            }
        });
    }
    
    public override void Dispose()
    {
        _downloadService.Dispose();
    }
}