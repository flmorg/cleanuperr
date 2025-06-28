using Data.Models.Arr.Queue;

namespace Cleanuparr.Domain.Entities.Arr.Queue;

public record QueueListResponse
{
    public required int TotalRecords { get; init; }
    public required IReadOnlyList<QueueRecord> Records { get; init; }
}