namespace Domain.Models.Arr;

public sealed record SonarrSearchItem : SearchItem
{
    public long SeriesId { get; set; }
}