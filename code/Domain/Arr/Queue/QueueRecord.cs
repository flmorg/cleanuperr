namespace Domain.Arr.Queue;

public record QueueRecord
{
    public int SeriesId { get; init; }
    public int MovieId { get; init; }
    public required string Title { get; init; }
    public string Status { get; init; }
    public string TrackedDownloadStatus { get; init; }
    public string TrackedDownloadState { get; init; }
    public required string DownloadId { get; init; }
    public required string Protocol { get; init; }
    public required int Id { get; init; }
}