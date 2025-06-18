namespace Cleanuparr.Domain.Entities.Lidarr;

public sealed record Artist
{
    public long Id { get; set; }
    
    public string ArtistName { get; set; }
}