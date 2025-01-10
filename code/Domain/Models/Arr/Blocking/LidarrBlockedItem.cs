namespace Domain.Models.Arr.Blocking;

public sealed record LidarrBlockedItem : BlockedItem
{
    public required long AlbumId { get; init; }
    
    public required long ArtistId { get; init; }
}