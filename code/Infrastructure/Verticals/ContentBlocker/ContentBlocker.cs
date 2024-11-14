using Common.Configuration;
using Domain.Arr.Queue;
using Domain.Enums;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.DownloadClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.ContentBlocker;

public sealed class ContentBlocker : IDisposable
{
    private readonly ILogger<ContentBlocker> _logger;
    private readonly SonarrConfig _sonarrConfig;
    private readonly RadarrConfig _radarrConfig;
    private readonly SonarrClient _sonarrClient;
    private readonly RadarrClient _radarrClient;
    private readonly ArrQueueIterator _arrArrQueueIterator;
    private readonly BlocklistProvider _blocklistProvider;
    private readonly IDownloadService _downloadService;
    
    public ContentBlocker(
        ILogger<ContentBlocker> logger,
        IOptions<SonarrConfig> sonarrConfig,
        IOptions<RadarrConfig> radarrConfig,
        SonarrClient sonarrClient,
        RadarrClient radarrClient,
        ArrQueueIterator arrArrQueueIterator,
        BlocklistProvider blocklistProvider,
        DownloadServiceFactory downloadServiceFactory
    )
    {
        _logger = logger;
        _sonarrConfig = sonarrConfig.Value;
        _radarrConfig = radarrConfig.Value;
        _sonarrClient = sonarrClient;
        _radarrClient = radarrClient;
        _arrArrQueueIterator = arrArrQueueIterator;
        _blocklistProvider = blocklistProvider;
        _downloadService = downloadServiceFactory.CreateDownloadClient();
    }

    public async Task ExecuteAsync()
    {
        await _blocklistProvider.LoadBlocklistAsync();
        await _downloadService.LoginAsync();

        await ProcessArrConfigAsync(_sonarrConfig, InstanceType.Sonarr);
        await ProcessArrConfigAsync(_radarrConfig, InstanceType.Radarr);
    }

    private async Task ProcessArrConfigAsync(ArrConfig config, InstanceType instanceType)
    {
        if (!config.Enabled)
        {
            return;
        }

        foreach (ArrInstance arrInstance in config.Instances)
        {
            try
            {
                await ProcessInstanceAsync(arrInstance, instanceType);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "failed to block content for {type} instance | {url}", instanceType, arrInstance.Url);
            }
        }
    }

    private async Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType)
    {
        ArrClient arrClient = GetClient(instanceType);

        await _arrArrQueueIterator.Iterate(arrClient, instance, async items =>
        {
            foreach (QueueRecord record in items)
            {
                _logger.LogDebug("searching unwanted files for {title}", record.Title);
                await _downloadService.BlockUnwantedFilesAsync(record.DownloadId);
            }
        });
    }
    
    private ArrClient GetClient(InstanceType type) =>
        type switch
        {
            InstanceType.Sonarr => _sonarrClient,
            InstanceType.Radarr => _radarrClient,
            _ => throw new NotImplementedException($"instance type {type} is not yet supported")
        };
    
    public void Dispose()
    {
        _downloadService.Dispose();
    }
}