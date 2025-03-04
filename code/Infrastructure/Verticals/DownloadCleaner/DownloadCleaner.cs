using Common.Configuration.Arr;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Domain.Enums;
using Domain.Models.Arr.Queue;
using Infrastructure.Providers;
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
    private readonly IgnoredDownloadsProvider<DownloadCleanerConfig> _ignoredDownloadsProvider;
    private readonly HashSet<string> _excludedHashes = [];
    
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
        INotificationPublisher notifier,
        IgnoredDownloadsProvider<DownloadCleanerConfig> ignoredDownloadsProvider
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
        _ignoredDownloadsProvider = ignoredDownloadsProvider;
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
        
        IReadOnlyList<string> ignoredDownloads = await _ignoredDownloadsProvider.GetIgnoredDownloads();
        
        await _downloadService.LoginAsync();

        List<object>? downloads = await _downloadService.GetAllDownloadsToBeCleaned(_config.Categories);

        if (downloads?.Count is null or 0)
        {
            _logger.LogDebug("no downloads found in the download client");
            return;
        }

        // wait for the downloads to appear in the arr queue
        await Task.Delay(10 * 1000);

        await ProcessArrConfigAsync(_sonarrConfig, InstanceType.Sonarr, true);
        await ProcessArrConfigAsync(_radarrConfig, InstanceType.Radarr, true);
        await ProcessArrConfigAsync(_lidarrConfig, InstanceType.Lidarr, true);
        
        await _downloadService.CleanDownloads(downloads, _config.Categories, _excludedHashes, ignoredDownloads);
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