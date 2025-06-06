using Common.Configuration.Arr;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Data.Enums;
using Data.Models.Arr.Queue;
using Infrastructure.Configuration;
using Infrastructure.Helpers;
using Infrastructure.Services;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.Arr.Interfaces;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.Jobs;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LogContext = Serilog.Context.LogContext;

namespace Infrastructure.Verticals.DownloadCleaner;

public sealed class DownloadCleaner : GenericHandler
{
    private readonly DownloadCleanerConfig _config;
    private readonly HashSet<string> _excludedHashes = [];
    
    private static bool _hardLinkCategoryCreated;
    
    public DownloadCleaner(
        ILogger<DownloadCleaner> logger,
        IConfigManager configManager,
        IMemoryCache cache,
        IBus messageBus,
        ArrClientFactory arrClientFactory,
        ArrQueueIterator arrArrQueueIterator,
        DownloadServiceFactory downloadServiceFactory
    ) : base(
        logger, cache, messageBus,
        arrClientFactory, arrArrQueueIterator, downloadServiceFactory, configManager
    )
    {
        _config = configManager.GetConfiguration<DownloadCleanerConfig>();
    }
    
    protected override void InitializeDownloadServices()
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
                var downloadService = _downloadServiceFactory.GetDownloadService(client);
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
        if (_downloadClientConfig.Clients.Count is 0)
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
        
        bool isUnlinkedEnabled = !string.IsNullOrEmpty(_config.UnlinkedTargetCategory) && _config.UnlinkedCategories.Count > 0;
        bool isCleaningEnabled = _config.Categories.Count > 0;
        
        if (!isUnlinkedEnabled && !isCleaningEnabled)
        {
            _logger.LogWarning("{name} is not configured properly", nameof(DownloadCleaner));
            return;
        }
        
        IReadOnlyList<string> ignoredDownloads = _generalConfig.IgnoredDownloads;
        
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
                    _logger.LogDebug("creating category {cat}", _config.UnlinkedTargetCategory);
                    await downloadService.CreateCategoryAsync(_config.UnlinkedTargetCategory);
                    // TODO mark creation as done
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
                        // TODO this is fucked up; I can't know which client the download belongs to
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
                    // TODO this is fucked up; I can't know which client the download belongs to
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
        using var _ = LogContext.PushProperty(LogProperties.Category, instanceType.ToString());
        
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