namespace Data.Models.Arr.Blocking;

public record BlockedItem
{
    public required string Hash { get; init; }
    
    public required Uri InstanceUrl { get; init; }
}