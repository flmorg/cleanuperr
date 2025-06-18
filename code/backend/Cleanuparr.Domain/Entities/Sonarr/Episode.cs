namespace Cleanuparr.Domain.Entities.Sonarr;

public sealed record Episode
{
    public long Id { get; set; }
    
    public int EpisodeNumber { get; set; }
            
    public int SeasonNumber { get; set; }
    
    public long SeriesId { get; set; }
}