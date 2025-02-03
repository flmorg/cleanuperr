using Common.Configuration.Arr;
using Common.Configuration.DownloadClient;
using Domain.Enums;
using Domain.Models.Arr.Queue;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.Jobs;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace Infrastructure.Verticals.DownloadCleaner;

public sealed class DownloadCleaner : GenericHandler
{
    private readonly HashSet<string> _excludedHashes = [];
    
    public DownloadCleaner(
        ILogger<DownloadCleaner> logger,
        // IOptions<Download> config, TODO
        IOptions<DownloadClientConfig> downloadClientConfig,
        IOptions<SonarrConfig> sonarrConfig,
        IOptions<RadarrConfig> radarrConfig,
        IOptions<LidarrConfig> lidarrConfig,
        SonarrClient sonarrClient,
        RadarrClient radarrClient,
        LidarrClient lidarrClient,
        ArrQueueIterator arrArrQueueIterator,
        DownloadServiceFactory downloadServiceFactory,
        NotificationPublisher notifier
    ) : base(
        logger, downloadClientConfig,
        sonarrConfig, radarrConfig, lidarrConfig,
        sonarrClient, radarrClient, lidarrClient,
        arrArrQueueIterator, downloadServiceFactory,
        notifier
    )
    {
        // _config = config.Value;
    }
    
    public override async Task ExecuteAsync()
    {
        await _downloadService.LoginAsync();

        await ProcessArrConfigAsync(_sonarrConfig, InstanceType.Sonarr, true);
        await ProcessArrConfigAsync(_radarrConfig, InstanceType.Radarr, true);
        await ProcessArrConfigAsync(_lidarrConfig, InstanceType.Lidarr, true);
        
        // TODO remove from download client by tag/category
    }

    protected override async Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType)
    {
        using var _ = LogContext.PushProperty("InstanceName", instanceType.ToString());
        
        ArrClient arrClient = GetClient(instanceType);
        
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