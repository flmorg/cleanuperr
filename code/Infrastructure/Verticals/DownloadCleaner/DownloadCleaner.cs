using Common.Configuration.Arr;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.General;
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

namespace Infrastructure.Verticals.DownloadCleaner;

public sealed class DownloadCleaner : GenericHandler
{
    private readonly HashSet<string> _excludedHashes = [];
    
    public DownloadCleaner(
        ILogger<DownloadCleaner> logger,
        DataContext dataContext,
        IMemoryCache cache,
        IBus messageBus,
        ArrClientFactory arrClientFactory,
        ArrQueueIterator arrArrQueueIterator,
        DownloadServiceFactory downloadServiceFactory,
        EventPublisher eventPublisher
    ) : base(
        logger, dataContext, cache, messageBus,
        arrClientFactory, arrArrQueueIterator, downloadServiceFactory, eventPublisher
    )
    {
    }
    
    // protected override void GetDownloadServices()
    // {
    //     // Clear existing services
    //     _downloadServices.Clear();
    //     
    //     if (_downloadClientConfigs.Clients.Count == 0)
    //     {
    //         _logger.LogWarning("No download clients configured");
    //         return;
    //     }
    //     
    //     foreach (var client in _downloadClientConfigs.GetEnabledClients())
    //     {
    //         try
    //         {
    //             var downloadService = _downloadServiceFactory.GetDownloadService(client);
    //             if (downloadService != null)
    //             {
    //                 _downloadServices.Add(downloadService);
    //                 _logger.LogDebug("Added download client: {name} ({id})", client.Name, client.Id);
    //             }
    //             else
    //             {
    //                 _logger.LogWarning("Download client service not available for: {id}", client.Id);
    //             }
    //         }
    //         catch (Exception ex)
    //         {
    //             _logger.LogError(ex, "Error initializing download client {id}: {message}", client.Id, ex.Message);
    //         }
    //     }
    // }
    
    protected override async Task ExecuteInternalAsync()
    {
        var downloadServices = await GetDownloadServices();

        if (downloadServices.Count is 0)
        {
            return;
        }

        var config = ContextProvider.Get<DownloadCleanerConfig>();
        
        bool isUnlinkedEnabled = config.UnlinkedEnabled && !string.IsNullOrEmpty(config.UnlinkedTargetCategory) && config.UnlinkedCategories.Count > 0;
        bool isCleaningEnabled = config.Categories.Count > 0;
        
        if (!isUnlinkedEnabled && !isCleaningEnabled)
        {
            _logger.LogWarning("{name} is not configured properly", nameof(DownloadCleaner));
            return;
        }
        
        IReadOnlyList<string> ignoredDownloads = ContextProvider.Get<GeneralConfig>(nameof(GeneralConfig)).IgnoredDownloads;
        
        // Process each client separately
        var allDownloads = new List<object>();
        foreach (var downloadService in downloadServices)
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
            foreach (var downloadService in downloadServices)
            {
                try
                {
                    _logger.LogDebug("creating category {cat}", config.UnlinkedTargetCategory);
                    await downloadService.CreateCategoryAsync(config.UnlinkedTargetCategory);
                    // TODO mark creation as done
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create category for download client");
                }
            }
            
            // Get downloads to change category
            downloadsToChangeCategory = new List<object>();
            foreach (var downloadService in downloadServices)
            {
                try
                {
                    var clientDownloads = downloadService.FilterDownloadsToChangeCategoryAsync(allDownloads, config.UnlinkedCategories);
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

        await ProcessArrConfigAsync(ContextProvider.Get<SonarrConfig>(), InstanceType.Sonarr, true);
        await ProcessArrConfigAsync(ContextProvider.Get<SonarrConfig>(), InstanceType.Radarr, true);
        await ProcessArrConfigAsync(ContextProvider.Get<SonarrConfig>(), InstanceType.Lidarr, true);

        if (isUnlinkedEnabled && downloadsToChangeCategory?.Count > 0)
        {
            _logger.LogTrace("found {count} potential downloads to change category", downloadsToChangeCategory.Count);
            
            // Process each client with its own filtered downloads
            foreach (var downloadService in downloadServices)
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
        
        if (config.Categories?.Count is null or 0)
        {
            return;
        }
        
        // Get downloads to clean
        List<object> downloadsToClean = new List<object>();
        foreach (var downloadService in downloadServices)
        {
            try
            {
                var clientDownloads = downloadService.FilterDownloadsToBeCleanedAsync(allDownloads, config.Categories);
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
        foreach (var downloadService in downloadServices)
        {
            try
            {
                await downloadService.CleanDownloadsAsync(downloadsToClean, config.Categories, _excludedHashes, ignoredDownloads);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean downloads");
            }
        }
        
        _logger.LogTrace("finished cleaning downloads");

        foreach (var downloadService in downloadServices)
        {
            downloadService.Dispose();
        }
    }

    protected override async Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType, ArrConfig arrConfig)
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
}