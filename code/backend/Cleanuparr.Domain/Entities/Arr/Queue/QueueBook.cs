namespace Cleanuparr.Domain.Entities.Arr.Queue;

public sealed record QueueBook
{
    public List<ReadarrImage> Images { get; init; } = [];
} 