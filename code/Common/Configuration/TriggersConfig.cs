namespace Common.Configuration;

public sealed class TriggersConfig
{
    public const string SectionName = "Triggers";
    
    public required string QueueCleaner { get; init; }
}