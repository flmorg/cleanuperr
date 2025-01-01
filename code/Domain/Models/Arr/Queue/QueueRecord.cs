namespace Domain.Models.Arr.Queue;

public record QueueRecord
{
    // Sonarr
    public int SeriesId { get; init; }
    public int EpisodeId { get; init; }
    public int SeasonNumber { get; init; }
    
    // Radarr
    public int MovieId { get; init; }
    
    // Lidarr
    public int ArtistId { get; init; }
    
    public int AlbumId { get; init; }
    
    // common
    public required string Title { get; init; }
    public string Status { get; init; }
    public string TrackedDownloadStatus { get; init; }
    public string TrackedDownloadState { get; init; }
    public required string DownloadId { get; init; }
    public required string Protocol { get; init; }
    public required int Id { get; init; }
}