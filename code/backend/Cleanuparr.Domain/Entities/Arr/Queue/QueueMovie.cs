namespace Cleanuparr.Domain.Entities.Arr.Queue;

public sealed record QueueMovie
{
    public List<Image> Images { get; init; } = [];
}