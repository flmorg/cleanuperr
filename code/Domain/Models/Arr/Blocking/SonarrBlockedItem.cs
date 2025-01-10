namespace Domain.Models.Arr.Blocking;

public sealed record SonarrBlockedItem : BlockedItem
{
    public required long EpisodeId { get; init; }
    
    public required long SeasonNumber { get; init; }
    
    public required long SeriesId { get; init; }
}