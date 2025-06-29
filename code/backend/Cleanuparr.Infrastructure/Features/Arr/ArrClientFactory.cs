using Cleanuparr.Domain.Enums;
using Cleanuparr.Infrastructure.Features.Arr.Interfaces;

namespace Cleanuparr.Infrastructure.Features.Arr;

public sealed class ArrClientFactory
{
    private readonly ISonarrClient _sonarrClient;
    private readonly IRadarrClient _radarrClient;
    private readonly ILidarrClient _lidarrClient;
    private readonly IReadarrClient _readarrClient;

    public ArrClientFactory(
        SonarrClient sonarrClient,
        RadarrClient radarrClient,
        LidarrClient lidarrClient,
        ReadarrClient readarrClient
    )
    {
        _sonarrClient = sonarrClient;
        _radarrClient = radarrClient;
        _lidarrClient = lidarrClient;
        _readarrClient = readarrClient;
    }
    
    public IArrClient GetClient(InstanceType type) =>
        type switch
        {
            InstanceType.Sonarr => _sonarrClient,
            InstanceType.Radarr => _radarrClient,
            InstanceType.Lidarr => _lidarrClient,
            InstanceType.Readarr => _readarrClient,
            _ => throw new NotImplementedException($"instance type {type} is not yet supported")
        };
}