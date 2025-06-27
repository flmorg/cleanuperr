namespace Cleanuparr.Domain.Entities.Lidarr;

public sealed record Album
{
    public long Id { get; set; }
    
    public string Title { get; set; }
    
    public long ArtistId { get; set; }
    
    public Artist Artist { get; set; }
}