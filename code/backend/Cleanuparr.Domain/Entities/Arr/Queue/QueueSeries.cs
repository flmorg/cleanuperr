namespace Cleanuparr.Domain.Entities.Arr.Queue;

public sealed record QueueSeries
{
    public List<Image> Images { get; init; } = [];
}