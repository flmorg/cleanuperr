using Common.Configuration;
using Domain.Arr.Queue;
using Domain.Enums;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.DownloadClient;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.Jobs;

public abstract class GenericHandler : IDisposable
{
    protected readonly ILogger<GenericHandler> _logger;
    protected readonly SonarrConfig _sonarrConfig;
    protected readonly RadarrConfig _radarrConfig;
    protected readonly SonarrClient _sonarrClient;
    protected readonly RadarrClient _radarrClient;
    protected readonly ArrQueueIterator _arrArrQueueIterator;
    protected readonly IDownloadService _downloadService;

    protected GenericHandler(
        ILogger<GenericHandler> logger,
        SonarrConfig sonarrConfig,
        RadarrConfig radarrConfig,
        SonarrClient sonarrClient,
        RadarrClient radarrClient,
        ArrQueueIterator arrArrQueueIterator,
        DownloadServiceFactory downloadServiceFactory
    )
    {
        _logger = logger;
        _sonarrConfig = sonarrConfig;
        _radarrConfig = radarrConfig;
        _sonarrClient = sonarrClient;
        _radarrClient = radarrClient;
        _arrArrQueueIterator = arrArrQueueIterator;
        _downloadService = downloadServiceFactory.CreateDownloadClient();
    }

    public virtual async Task ExecuteAsync()
    {
        await _downloadService.LoginAsync();

        await ProcessArrConfigAsync(_sonarrConfig, InstanceType.Sonarr);
        await ProcessArrConfigAsync(_radarrConfig, InstanceType.Radarr);
    }

    public virtual void Dispose()
    {
        _downloadService.Dispose();
    }

    protected abstract Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType);
    
    protected async Task ProcessArrConfigAsync(ArrConfig config, InstanceType instanceType)
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
    
    protected ArrClient GetClient(InstanceType type) =>
        type switch
        {
            InstanceType.Sonarr => _sonarrClient,
            InstanceType.Radarr => _radarrClient,
            _ => throw new NotImplementedException($"instance type {type} is not yet supported")
        };
    
    protected int GetRecordId(InstanceType type, QueueRecord record) =>
        type switch
        {
            // TODO add episode id
            InstanceType.Sonarr => record.SeriesId,
            InstanceType.Radarr => record.MovieId,
            _ => throw new NotImplementedException($"instance type {type} is not yet supported")
        };
}