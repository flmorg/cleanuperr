namespace Common.Configuration.QueueCleaner;

public sealed record QueueCleanerConfig
{
    public const string SectionName = "QueueCleaner";
    
    public required bool Enabled { get; init; }
}