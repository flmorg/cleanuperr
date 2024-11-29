namespace Domain.Models.Sonarr;

public sealed record SonarrCommand
{
    public string Name { get; set; }

    public long? SeriesId { get; set; }
    
    public long? SeasonNumber { get; set; }
    
    public List<long>? EpisodeIds { get; set; }
}