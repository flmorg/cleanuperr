namespace Common.Configuration.QueueCleaner;

public sealed record QueueCleanerConfig : IJobConfig
{
    public const string SectionName = "QueueCleaner";
    
    public required bool Enabled { get; init; }
    
    public required bool RunSequentially { get; init; }

    public void Validate()
    {
    }
}