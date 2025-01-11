using Microsoft.Extensions.Configuration;

namespace Common.Configuration.QueueCleaner;

public sealed record QueueCleanerConfig : IJobConfig
{
    public const string SectionName = "QueueCleaner";
    
    public required bool Enabled { get; init; }
    
    public required bool RunSequentially { get; init; }
    
    [ConfigurationKeyName("IMPORT_FAILED_MAX_STRIKES")]
    public ushort ImportFailedMaxStrikes { get; init; }
    
    [ConfigurationKeyName("IMPORT_FAILED_IGNORE_PRIVATE")]
    public bool ImportFailedIgnorePrivate { get; init; }
    
    [ConfigurationKeyName("IMPORT_FAILED_IGNORE_PATTERNS")]
    public List<string>? ImportFailedIgnorePatterns { get; init; }
    
    [ConfigurationKeyName("STALLED_MAX_STRIKES")]
    public ushort StalledMaxStrikes { get; init; }
    
    [ConfigurationKeyName("STALLED_IGNORE_PRIVATE")]
    public bool StalledIgnorePrivate { get; init; }

    public void Validate()
    {
    }
}