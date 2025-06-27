using Cleanuparr.Domain.Enums;

namespace Cleanuparr.Domain.Entities.Sonarr;

public sealed record SonarrCommand
{
    public string Name { get; set; }

    public long? SeriesId { get; set; }
    
    public long? SeasonNumber { get; set; }
    
    public List<long>? EpisodeIds { get; set; }
    
    public SonarrSearchType SearchType { get; set; }
}