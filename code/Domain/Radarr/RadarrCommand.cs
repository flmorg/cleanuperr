namespace Domain.Radarr;

public sealed record RadarrCommand
{
    public required string Name { get; init; }
    
    public required HashSet<int> MovieIds { get; init; }
}