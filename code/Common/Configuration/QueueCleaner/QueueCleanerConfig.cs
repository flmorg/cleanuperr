using Microsoft.Extensions.Configuration;

namespace Common.Configuration.QueueCleaner;

public sealed record QueueCleanerConfig : IJobConfig
{
    public const string SectionName = "QueueCleaner";
    
    public required bool Enabled { get; init; }
    
    public required bool RunSequentially { get; init; }
    
    [ConfigurationKeyName("IMPORT_FAILED_MAX_STRIKES")]
    public ushort ImportFailedMaxStrikes { get; init; }
    
    [ConfigurationKeyName("STALLED_MAX_STRIKES")]
    public ushort StalledMaxStrikes { get; init; }

    public void Validate()
    {
    }
}