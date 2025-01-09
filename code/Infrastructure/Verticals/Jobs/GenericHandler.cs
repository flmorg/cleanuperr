using Common.Configuration.Arr;
using Common.Configuration.DownloadClient;
using Domain.Enums;
using Domain.Models.Arr;
using Domain.Models.Arr.Queue;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.DownloadClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.Jobs;

public abstract class GenericHandler : IDisposable
{
    protected readonly ILogger<GenericHandler> _logger;
    protected readonly DownloadClientConfig _downloadClientConfig;
    protected readonly SonarrConfig _sonarrConfig;
    protected readonly RadarrConfig _radarrConfig;
    protected readonly LidarrConfig _lidarrConfig;
    protected readonly SonarrClient _sonarrClient;
    protected readonly RadarrClient _radarrClient;
    protected readonly LidarrClient _lidarrClient;
    protected readonly ArrQueueIterator _arrArrQueueIterator;
    protected readonly IDownloadService _downloadService;

    protected GenericHandler(
        ILogger<GenericHandler> logger,
        IOptions<DownloadClientConfig> downloadClientConfig,
        IOptions<SonarrConfig> sonarrConfig,
        IOptions<RadarrConfig> radarrConfig,
        IOptions<LidarrConfig> lidarrConfig,
        SonarrClient sonarrClient,
        RadarrClient radarrClient,
        LidarrClient lidarrClient,
        ArrQueueIterator arrArrQueueIterator,
        DownloadServiceFactory downloadServiceFactory
    )
    {
        _logger = logger;
        _downloadClientConfig = downloadClientConfig.Value;
        _sonarrConfig = sonarrConfig.Value;
        _radarrConfig = radarrConfig.Value;
        _lidarrConfig = lidarrConfig.Value;
        _sonarrClient = sonarrClient;
        _radarrClient = radarrClient;
        _lidarrClient = lidarrClient;
        _arrArrQueueIterator = arrArrQueueIterator;
        _downloadService = downloadServiceFactory.CreateDownloadClient();
    }

    public virtual async Task ExecuteAsync()
    {
        await _downloadService.LoginAsync();

        await ProcessArrConfigAsync(_sonarrConfig, InstanceType.Sonarr);
        await ProcessArrConfigAsync(_radarrConfig, InstanceType.Radarr);
        await ProcessArrConfigAsync(_lidarrConfig, InstanceType.Lidarr);
    }

    public virtual void Dispose()
    {
        _downloadService.Dispose();
    }

    protected abstract Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType);
    
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
    
    protected ArrClient GetClient(InstanceType type) =>
        type switch
        {
            InstanceType.Sonarr => _sonarrClient,
            InstanceType.Radarr => _radarrClient,
            InstanceType.Lidarr => _lidarrClient,
            _ => throw new NotImplementedException($"instance type {type} is not yet supported")
        };

    protected SearchItem GetRecordSearchItem(InstanceType type, QueueRecord record, bool isPack = false)
    {
        return type switch
        {
            InstanceType.Sonarr when _sonarrConfig.SearchType is SonarrSearchType.Episode && !isPack => new SonarrSearchItem
            {
                Id = record.EpisodeId,
                SeriesId = record.SeriesId,
                SearchType = SonarrSearchType.Episode
            },
            InstanceType.Sonarr when _sonarrConfig.SearchType is SonarrSearchType.Episode && isPack => new SonarrSearchItem
            {
                Id = record.SeasonNumber,
                SeriesId = record.SeriesId,
                SearchType = SonarrSearchType.Season
            },
            InstanceType.Sonarr when _sonarrConfig.SearchType is SonarrSearchType.Season => new SonarrSearchItem
            {
                Id = record.SeasonNumber,
                SeriesId = record.SeriesId,
                SearchType = SonarrSearchType.Series
            },
            InstanceType.Sonarr when _sonarrConfig.SearchType is SonarrSearchType.Series => new SonarrSearchItem
            {
                Id = record.SeriesId
            },
            InstanceType.Radarr => new SearchItem
            {
                Id = record.MovieId
            },
            InstanceType.Lidarr => new SearchItem
            {
                Id = record.AlbumId
            },
            _ => throw new NotImplementedException($"instance type {type} is not yet supported")
        };
    }
}