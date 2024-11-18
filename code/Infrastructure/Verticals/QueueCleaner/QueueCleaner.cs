using Common.Configuration;
using Domain.Arr.Queue;
using Domain.Enums;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.DownloadClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.QueueCleaner;

public sealed class QueueCleaner : IDisposable
{
    private readonly ILogger<QueueCleaner> _logger;
    private readonly SonarrConfig _sonarrConfig;
    private readonly RadarrConfig _radarrConfig;
    private readonly SonarrClient _sonarrClient;
    private readonly RadarrClient _radarrClient;
    private readonly ArrQueueIterator _arrArrQueueIterator;
    private readonly IDownloadService _downloadService;
    
    public QueueCleaner(
        ILogger<QueueCleaner> logger,
        IOptions<SonarrConfig> sonarrConfig,
        IOptions<RadarrConfig> radarrConfig,
        SonarrClient sonarrClient,
        RadarrClient radarrClient,
        ArrQueueIterator arrArrQueueIterator,
        DownloadServiceFactory downloadServiceFactory
    )
    {
        _logger = logger;
        _sonarrConfig = sonarrConfig.Value;
        _radarrConfig = radarrConfig.Value;
        _sonarrClient = sonarrClient;
        _radarrClient = radarrClient;
        _arrArrQueueIterator = arrArrQueueIterator;
        _downloadService = downloadServiceFactory.CreateDownloadClient();
    }
    
    public async Task ExecuteAsync()
    {
        await _downloadService.LoginAsync();

        await ProcessArrConfigAsync(_sonarrConfig, InstanceType.Sonarr);
        await ProcessArrConfigAsync(_radarrConfig, InstanceType.Radarr);
        
        // await _downloadClient.LogoutAsync();
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
                _logger.LogError(exception, "failed to clean {type} instance | {url}", instanceType, arrInstance.Url);
            }
        }
    }

    private async Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType)
    {
        HashSet<int> itemsToBeRefreshed = [];
        ArrClient arrClient = GetClient(instanceType);

        await _arrArrQueueIterator.Iterate(arrClient, instance, async items =>
        {
            foreach (QueueRecord record in items)
            {
                if (record.Protocol is not "torrent")
                {
                    continue;
                }

                if (string.IsNullOrEmpty(record.DownloadId))
                {
                    _logger.LogDebug("skip | download id is null for {title}", record.Title);
                    continue;
                }

                if (!await _downloadService.ShouldRemoveFromArrQueueAsync(record.DownloadId))
                {
                    _logger.LogInformation("skip | {title}", record.Title);
                    continue;
                }

                itemsToBeRefreshed.Add(GetRecordId(instanceType, record));

                await arrClient.DeleteQueueItemAsync(instance, record);
            }
        });
        
        await arrClient.RefreshItemsAsync(instance, itemsToBeRefreshed);
    }

    private ArrClient GetClient(InstanceType type) =>
        type switch
        {
            InstanceType.Sonarr => _sonarrClient,
            InstanceType.Radarr => _radarrClient,
            _ => throw new NotImplementedException($"instance type {type} is not yet supported")
        };
    
    private int GetRecordId(InstanceType type, QueueRecord record) =>
        type switch
        {
            // TODO add episode id
            InstanceType.Sonarr => record.SeriesId,
            InstanceType.Radarr => record.MovieId,
            _ => throw new NotImplementedException($"instance type {type} is not yet supported")
        };

    public void Dispose()
    {
        _downloadService.Dispose();
    }
}