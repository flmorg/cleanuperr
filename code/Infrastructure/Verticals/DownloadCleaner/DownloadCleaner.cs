using Common.Configuration.Arr;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Domain.Enums;
using Domain.Models.Arr.Queue;
using Infrastructure.Configuration;
using Infrastructure.Providers;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.Arr.Interfaces;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.Jobs;
using Infrastructure.Verticals.Notifications;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LogContext = Serilog.Context.LogContext;

namespace Infrastructure.Verticals.DownloadCleaner;

public sealed class DownloadCleaner : GenericHandler
{
    private readonly DownloadCleanerConfig _config;
    private readonly IgnoredDownloadsProvider<DownloadCleanerConfig> _ignoredDownloadsProvider;
    private readonly HashSet<string> _excludedHashes = [];
    private readonly IConfigurationManager _configManager;
    
    private static bool _hardLinkCategoryCreated;
    
    public DownloadCleaner(
        ILogger<DownloadCleaner> logger,
        IConfigurationManager configManager,
        IMemoryCache cache,
        IBus messageBus,
        ArrClientFactory arrClientFactory,
        ArrQueueIterator arrArrQueueIterator,
        DownloadServiceFactory downloadServiceFactory,
        INotificationPublisher notifier,
        IgnoredDownloadsProvider<DownloadCleanerConfig> ignoredDownloadsProvider
    ) : base(
        logger, cache, messageBus,
        arrClientFactory, arrArrQueueIterator, downloadServiceFactory,
        notifier
    )
    {
        _configManager = configManager;
        _ignoredDownloadsProvider = ignoredDownloadsProvider;
        
        // Initialize the configuration
        var configTask = _configManager.GetDownloadCleanerConfigAsync();
        configTask.Wait();
        _config = configTask.Result ?? new DownloadCleanerConfig();
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
    }
    
    public override async Task ExecuteAsync()
    {
        // Refresh configurations before executing
        await InitializeConfigs();
        
        if (_downloadClientConfig.DownloadClient is Common.Enums.DownloadClient.None or Common.Enums.DownloadClient.Disabled)
        {
            _logger.LogWarning("download client is not set");
            return;
        }
        
        bool isUnlinkedEnabled = !string.IsNullOrEmpty(_config.UnlinkedTargetCategory) && _config.UnlinkedCategories?.Count > 0;
        bool isCleaningEnabled = _config.Categories?.Count > 0;
        
        if (!isUnlinkedEnabled && !isCleaningEnabled)
        {
            _logger.LogWarning("{name} is not configured properly", nameof(DownloadCleaner));
            return;
        }
        
        IReadOnlyList<string> ignoredDownloads = await _ignoredDownloadsProvider.GetIgnoredDownloads();
        
        await _downloadService.LoginAsync();
        List<object>? downloads = await _downloadService.GetSeedingDownloads();

        if (downloads?.Count is null or 0)
        {
            _logger.LogDebug("no seeding downloads found");
            return;
        }
        
        _logger.LogTrace("found {count} seeding downloads", downloads.Count);
        
        List<object>? downloadsToChangeCategory = null;

        if (isUnlinkedEnabled)
        {
            if (!_hardLinkCategoryCreated)
            {
                if (_downloadClientConfig.DownloadClient is Common.Enums.DownloadClient.QBittorrent && !_config.UnlinkedUseTag)
                {
                    _logger.LogDebug("creating category {cat}", _config.UnlinkedTargetCategory);
                    await _downloadService.CreateCategoryAsync(_config.UnlinkedTargetCategory);
                }
                
                _hardLinkCategoryCreated = true;
            }
            
            downloadsToChangeCategory = _downloadService.FilterDownloadsToChangeCategoryAsync(downloads, _config.UnlinkedCategories);
        }

        // wait for the downloads to appear in the arr queue
        await Task.Delay(10 * 1000);

        await ProcessArrConfigAsync(_sonarrConfig, InstanceType.Sonarr, true);
        await ProcessArrConfigAsync(_radarrConfig, InstanceType.Radarr, true);
        await ProcessArrConfigAsync(_lidarrConfig, InstanceType.Lidarr, true);

        if (isUnlinkedEnabled)
        {
            _logger.LogTrace("found {count} potential downloads to change category", downloadsToChangeCategory?.Count);
            await _downloadService.ChangeCategoryForNoHardLinksAsync(downloadsToChangeCategory, _excludedHashes, ignoredDownloads);
            _logger.LogTrace("finished changing category");
        }
        
        if (_config.Categories?.Count is null or 0)
        {
            return;
        }
        
        List<object>? downloadsToClean = _downloadService.FilterDownloadsToBeCleanedAsync(downloads, _config.Categories);
        
        // release unused objects
        downloads = null;

        _logger.LogTrace("found {count} potential downloads to clean", downloadsToClean?.Count);
        await _downloadService.CleanDownloadsAsync(downloadsToClean, _config.Categories, _excludedHashes, ignoredDownloads);
        _logger.LogTrace("finished cleaning downloads");
    }

    protected override async Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType, ArrConfig config)
    {
        using var _ = LogContext.PushProperty("InstanceName", instanceType.ToString());
        
        IArrClient arrClient = _arrClientFactory.GetClient(instanceType);
        
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