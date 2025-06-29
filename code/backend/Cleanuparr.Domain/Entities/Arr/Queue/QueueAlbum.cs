namespace Cleanuparr.Domain.Entities.Arr.Queue;

public sealed record QueueAlbum
{
    public List<LidarrImage> Images { get; init; } = [];
}