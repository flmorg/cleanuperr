namespace Domain.Models.Sonarr;

public sealed record Series
{
    public required long Id { get; init; }
            
    public required string Title { get; init; }
}