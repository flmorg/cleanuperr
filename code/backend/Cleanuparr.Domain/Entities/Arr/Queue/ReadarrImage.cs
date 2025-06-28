namespace Cleanuparr.Domain.Entities.Arr.Queue;

public sealed record ReadarrImage
{
    public required string CoverType { get; init; }
    
    public required Uri Url { get; init; }
}