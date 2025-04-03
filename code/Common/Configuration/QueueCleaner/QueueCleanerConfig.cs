using Common.CustomDataTypes;
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
    
    [ConfigurationKeyName("SLOW_MAX_STRIKES")]
    public ushort SlowMaxStrikes { get; init; }
    
    [ConfigurationKeyName("SLOW_RESET_STRIKES_ON_PROGRESS")]
    public bool SlowResetStrikesOnProgress { get; init; }

    [ConfigurationKeyName("SLOW_IGNORE_PRIVATE")]
    public bool SlowIgnorePrivate { get; init; }
    
    [ConfigurationKeyName("SLOW_DELETE_PRIVATE")]
    public bool SlowDeletePrivate { get; init; }

    [ConfigurationKeyName("SLOW_MIN_SPEED")]
    public string SlowMinSpeed { get; init; } = string.Empty;
    
    public ByteSize SlowMinSpeedByteSize => string.IsNullOrEmpty(SlowMinSpeed) ? new ByteSize(0) : ByteSize.Parse(SlowMinSpeed);
    
    [ConfigurationKeyName("SLOW_MAX_TIME")]
    public double SlowMaxTime { get; init; }
    
    [ConfigurationKeyName("SLOW_IGNORE_ABOVE_SIZE")]
    public string SlowIgnoreAboveSize { get; init; } = string.Empty;
    
    public ByteSize? SlowIgnoreAboveSizeByteSize => string.IsNullOrEmpty(SlowIgnoreAboveSize) ? null : ByteSize.Parse(SlowIgnoreAboveSize);

    public void Validate()
    {
        if (ImportFailedMaxStrikes is > 0 and < 3)
        {
            throw new ValidationException($"the minimum value for {SectionName}__IMPORT_FAILED_MAX_STRIKES must be 3");
        }

        if (StalledMaxStrikes is > 0 and < 3)
        {
            throw new ValidationException($"the minimum value for {SectionName}__STALLED_MAX_STRIKES must be 3");
        }
        
        if (SlowMaxStrikes is > 0 and < 3)
        {
            throw new ValidationException($"the minimum value for {SectionName}__SLOW_MAX_STRIKES must be 3");
        }

        if (SlowMaxStrikes > 0)
        {
            bool isSlowSpeedSet = !string.IsNullOrEmpty(SlowMinSpeed);

            if (isSlowSpeedSet && ByteSize.TryParse(SlowMinSpeed, out _) is false)
            {
                throw new ValidationException($"invalid value for {SectionName}__SLOW_MIN_SPEED");
            }

            if (SlowMaxTime < 0)
            {
                throw new ValidationException($"invalid value for {SectionName}__SLOW_MAX_TIME");
            }

            if (!isSlowSpeedSet && SlowMaxTime is 0)
            {
                throw new ValidationException($"either {SectionName}__SLOW_MIN_SPEED or {SectionName}__SLOW_MAX_STRIKES must be set");
            }
        
            bool isSlowIgnoreAboveSizeSet = !string.IsNullOrEmpty(SlowIgnoreAboveSize);
        
            if (isSlowIgnoreAboveSizeSet && ByteSize.TryParse(SlowIgnoreAboveSize, out _) is false)
            {
                throw new ValidationException($"invalid value for {SectionName}__SLOW_IGNORE_ABOVE_SIZE");
            }
        }
    }
}