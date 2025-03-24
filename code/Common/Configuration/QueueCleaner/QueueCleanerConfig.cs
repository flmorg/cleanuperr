using Common.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Common.Configuration.QueueCleaner;

public sealed record QueueCleanerConfig : IJobConfig, IIgnoredDownloadsConfig
{
    public const string SectionName = "QueueCleaner";
    
    public required bool Enabled { get; init; }
    
    public required bool RunSequentially { get; init; }
    
    [ConfigurationKeyName("IGNORED_DOWNLOADS_PATH")]
    public string? IgnoredDownloadsPath { get; init; }
    
    [ConfigurationKeyName("IMPORT_FAILED_MAX_STRIKES")]
    public ushort ImportFailedMaxStrikes { get; init; }
    
    [ConfigurationKeyName("IMPORT_FAILED_IGNORE_PRIVATE")]
    public bool ImportFailedIgnorePrivate { get; init; }
    
    [ConfigurationKeyName("IMPORT_FAILED_DELETE_PRIVATE")]
    public bool ImportFailedDeletePrivate { get; init; }
    
    [ConfigurationKeyName("IMPORT_FAILED_IGNORE_PATTERNS")]
    public List<string>? ImportFailedIgnorePatterns { get; init; }
    
    [ConfigurationKeyName("STALLED_MAX_STRIKES")]
    public ushort StalledMaxStrikes { get; init; }
    
    [ConfigurationKeyName("STALLED_RESET_STRIKES_ON_PROGRESS")]
    public bool StalledResetStrikesOnProgress { get; init; }
    
    [ConfigurationKeyName("STALLED_IGNORE_PRIVATE")]
    public bool StalledIgnorePrivate { get; init; }
    
    [ConfigurationKeyName("STALLED_DELETE_PRIVATE")]
    public bool StalledDeletePrivate { get; init; }

    public void Validate()
    {
        if (ImportFailedMaxStrikes is > 0 and < 3)
        {
            throw new ValidationException("the minimum value for IMPORT_FAILED_MAX_STRIKES must be 3");
        }

        if (StalledMaxStrikes is > 0 and < 3)
        {
            throw new ValidationException("the minimum value for STALLED_MAX_STRIKES must be 3");
        }
    }
}