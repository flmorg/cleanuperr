namespace Cleanuparr.Domain.Entities.Arr.Queue;

public record LidarrImage
{
    public required string CoverType { get; init; }
    
    public required Uri Url { get; init; }
}