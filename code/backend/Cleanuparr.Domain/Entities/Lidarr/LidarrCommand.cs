namespace Cleanuparr.Domain.Entities.Lidarr;

public sealed record LidarrCommand
{
    public string Name { get; set; }
    
    public List<long> AlbumIds { get; set; }
    
    public long ArtistId { get; set; }
}