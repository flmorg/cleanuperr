using Common.Configuration.Arr;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Domain.Enums;
using Domain.Models.Arr.Queue;
using Infrastructure.Configuration;
using Infrastructure.Services;
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
    private readonly IIgnoredDownloadsService _ignoredDownloadsService;
    private readonly HashSet<string> _excludedHashes = [];
    private readonly IConfigManager _configManager;
    private readonly List<IDownloadService> _downloadServices = [];
    
    private static bool _hardLinkCategoryCreated;
    
    public DownloadCleaner(
        ILogger<DownloadCleaner> logger,
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
        _ignoredDownloadsService = ignoredDownloadsService;
        
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
    
    private void InitializeDownloadServices()
    {
        // Clear existing services
        _downloadServices.Clear();
        
        if (_downloadClientConfig.Clients.Count == 0)
        {
            _logger.LogWarning("No download clients configured");
            return;
        }
        
        foreach (var client in _downloadClientConfig.GetEnabledClients())
        {
            try
            {
                var downloadService = _downloadServiceFactory.GetDownloadService(client.Id);
                if (downloadService != null)
                {
                    _downloadServices.Add(downloadService);
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
    
    public override async Task ExecuteAsync()
    {
        // Refresh configurations before executing
        await InitializeConfigs();
        
        if (_downloadClientConfig.Clients.Count == 0)
        {
            _logger.LogWarning("No download clients configured");
            return;
        }
        
        // Initialize download services
        InitializeDownloadServices();
        
        if (_downloadServices.Count == 0)
        {
            _logger.LogWarning("No enabled download clients available");
            return;
        }
        
        bool isUnlinkedEnabled = !string.IsNullOrEmpty(_config.UnlinkedTargetCategory) && _config.UnlinkedCategories?.Count > 0;
        bool isCleaningEnabled = _config.Categories?.Count > 0;
        
        if (!isUnlinkedEnabled && !isCleaningEnabled)
        {
            _logger.LogWarning("{name} is not configured properly", nameof(DownloadCleaner));
            return;
        }
        
        IReadOnlyList<string> ignoredDownloads = await _ignoredDownloadsService.GetIgnoredDownloadsAsync();
        
        // Process each client separately
        var allDownloads = new List<object>();
        foreach (var downloadService in _downloadServices)
        {
            try
            {
                await downloadService.LoginAsync();
                var clientDownloads = await downloadService.GetSeedingDownloads();
                if (clientDownloads?.Count > 0)
                {
                    allDownloads.AddRange(clientDownloads);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get seeding downloads from download client");
            }
        }

        if (allDownloads.Count == 0)
        {
            _logger.LogDebug("no seeding downloads found");
            return;
        }
        
        _logger.LogTrace("found {count} seeding downloads", allDownloads.Count);
        
        List<object>? downloadsToChangeCategory = null;

        if (isUnlinkedEnabled)
        {
            // Create category for all clients
            foreach (var downloadService in _downloadServices)
            {
                try
                {
                    if (_downloadClientConfig.Clients.Any(x => x.Type == Common.Enums.DownloadClient.QBittorrent) && !_config.UnlinkedUseTag)
                    {
                        _logger.LogDebug("creating category {cat}", _config.UnlinkedTargetCategory);
                        await downloadService.CreateCategoryAsync(_config.UnlinkedTargetCategory);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create category for download client");
                }
            }
            
            // Get downloads to change category
            downloadsToChangeCategory = new List<object>();
            foreach (var downloadService in _downloadServices)
            {
                try
                {
                    var clientDownloads = downloadService.FilterDownloadsToChangeCategoryAsync(allDownloads, _config.UnlinkedCategories);
                    if (clientDownloads?.Count > 0)
                    {
                        downloadsToChangeCategory.AddRange(clientDownloads);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to filter downloads for category change");
                }
            }
        }

        // wait for the downloads to appear in the arr queue
        await Task.Delay(10 * 1000);

        await ProcessArrConfigAsync(_sonarrConfig, InstanceType.Sonarr, true);
        await ProcessArrConfigAsync(_radarrConfig, InstanceType.Radarr, true);
        await ProcessArrConfigAsync(_lidarrConfig, InstanceType.Lidarr, true);

        if (isUnlinkedEnabled && downloadsToChangeCategory?.Count > 0)
        {
            _logger.LogTrace("found {count} potential downloads to change category", downloadsToChangeCategory.Count);
            
            // Process each client with its own filtered downloads
            foreach (var downloadService in _downloadServices)
            {
                try
                {
                    await downloadService.ChangeCategoryForNoHardLinksAsync(downloadsToChangeCategory, _excludedHashes, ignoredDownloads);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to change category for downloads");
                }
            }
            
            _logger.LogTrace("finished changing category");
        }
        
        if (_config.Categories?.Count is null or 0)
        {
            return;
        }
        
        // Get downloads to clean
        List<object> downloadsToClean = new List<object>();
        foreach (var downloadService in _downloadServices)
        {
            try
            {
                var clientDownloads = downloadService.FilterDownloadsToBeCleanedAsync(allDownloads, _config.Categories);
                if (clientDownloads?.Count > 0)
                {
                    downloadsToClean.AddRange(clientDownloads);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to filter downloads for cleaning");
            }
        }
        
        // release unused objects
        allDownloads = null;

        _logger.LogTrace("found {count} potential downloads to clean", downloadsToClean.Count);
        
        // Process cleaning for each client
        foreach (var downloadService in _downloadServices)
        {
            try
            {
                await downloadService.CleanDownloadsAsync(downloadsToClean, _config.Categories, _excludedHashes, ignoredDownloads);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean downloads");
            }
        }
        
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
        foreach (var downloadService in _downloadServices)
        {
            downloadService.Dispose();
        }
    }
}