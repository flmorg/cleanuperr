using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.Arr;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadClient;
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

namespace Infrastructure.Verticals.ContentBlocker;

public sealed class ContentBlocker : GenericHandler
{
    private ContentBlockerConfig _config;
    private readonly BlocklistProvider _blocklistProvider;
    private readonly IIgnoredDownloadsService _ignoredDownloadsService;
    private readonly IConfigurationManager _configManager;
    private readonly IEnumerable<IDownloadService> _downloadServices;

    public ContentBlocker(
        ILogger<ContentBlocker> logger,
        IConfigurationManager configManager,
        IMemoryCache cache,
        IBus messageBus,
        ArrClientFactory arrClientFactory,
        ArrQueueIterator arrArrQueueIterator,
        BlocklistProvider blocklistProvider,
        IEnumerable<IDownloadService> downloadServices,
        INotificationPublisher notifier,
        IIgnoredDownloadsService ignoredDownloadsService,
        DownloadServiceFactory downloadServiceFactory
    ) : base(
        logger, cache, messageBus, 
        arrClientFactory, arrArrQueueIterator, 
        downloadServiceFactory, notifier
    )
    {
        _configManager = configManager;
        _blocklistProvider = blocklistProvider;
        _ignoredDownloadsService = ignoredDownloadsService;
        _downloadServices = downloadServices;
        
        // Initialize the configuration
        var configTask = _configManager.GetContentBlockerConfigAsync();
        configTask.Wait();
        _config = configTask.Result ?? new ContentBlockerConfig();
        
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
        
        if (_downloadClientConfig.Clients.Count == 0)
        {
            _logger.LogWarning("No download clients configured");
            return;
        }
        
        // Update the content blocker configuration as well
        var contentBlockerConfigTask = _configManager.GetContentBlockerConfigAsync();
        await contentBlockerConfigTask;
        _config = contentBlockerConfigTask.Result ?? _config;
        
        bool blocklistIsConfigured = _config.Sonarr.Enabled && !string.IsNullOrEmpty(_config.Sonarr.Path) ||
                                     _config.Radarr.Enabled && !string.IsNullOrEmpty(_config.Radarr.Path) ||
                                     _config.Lidarr.Enabled && !string.IsNullOrEmpty(_config.Lidarr.Path);

        if (!blocklistIsConfigured)
        {
            _logger.LogWarning("no blocklist is configured");
            return;
        }
        
        await _blocklistProvider.LoadBlocklistsAsync();
        await base.ExecuteAsync();
    }

    protected override async Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType, ArrConfig config)
    {
        IReadOnlyList<string> ignoredDownloads = await _ignoredDownloadsService.GetIgnoredDownloadsAsync();
        
        using var _ = LogContext.PushProperty("InstanceName", instanceType.ToString());

        IArrClient arrClient = _arrClientFactory.GetClient(instanceType);
        BlocklistType blocklistType = _blocklistProvider.GetBlocklistType(instanceType);
        ConcurrentBag<string> patterns = _blocklistProvider.GetPatterns(instanceType);
        ConcurrentBag<Regex> regexes = _blocklistProvider.GetRegexes(instanceType);

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
                string hash = record.DownloadId;
                
                // Skip this record if it is in the ignored list
                if (ignoredDownloads.Contains(hash, StringComparer.InvariantCultureIgnoreCase))
                {
                    _logger.LogDebug("skipping {name} | download id is in ignore list", record.Title);
                    continue;
                }
                
                _logger.LogTrace("processing | {name}", record.Title);
                
                // Process through all download clients
                bool foundInAnyClient = false;
                foreach (var downloadService in _downloadServices)
                {
                    try
                    {
                        var result = await downloadService.BlockUnwantedFilesAsync(
                            hash, 
                            blocklistType, 
                            patterns, 
                            regexes, 
                            ignoredDownloads);
                        
                        if (result.Processed)
                        {
                            foundInAnyClient = true;
                            
                            // Successfully processed by this client, log results
                            if (result.HasHardLinks)
                            {
                                _logger.LogInformation(
                                    "skipping hard linked files | {title} | {paths}", 
                                    record.Title, 
                                    string.Join(", ", result.HardLinkedFiles));
                            }
                            
                            if (result.BlockedFiles.Count > 0)
                            {
                                _notifier.BlockedFiles(
                                    record.Title, 
                                    instanceType.ToString(), 
                                    result.BlockedFiles, 
                                    _config.BlockDeleteAfter);
                            }
                            
                            // Break after the first client successfully processes this download
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex, 
                            "Error blocking unwanted files for {hash} with download client", 
                            hash);
                    }
                }
                
                if (!foundInAnyClient)
                {
                    _logger.LogWarning(
                        "Download {hash} ({title}) not found in any download client", 
                        hash, 
                        record.Title);
                }
            }
        });
    }
}