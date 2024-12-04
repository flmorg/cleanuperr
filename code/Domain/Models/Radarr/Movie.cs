namespace Domain.Models.Radarr;

public sealed record Movie
{
    public required long Id { get; init; }
    
    public required string Title { get; init; }
}