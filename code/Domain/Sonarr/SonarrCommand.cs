namespace Domain.Sonarr;

public sealed record SonarrCommand
{
    public required string Name { get; init; }

    public required int SeriesId { get; set; }
}