using Common.CustomDataTypes;
using Common.Exceptions;
using Newtonsoft.Json;

namespace Common.Configuration.QueueCleaner;

public sealed record QueueCleanerConfig : IJobConfig
{
    public const string SectionName = "QueueCleaner";
    
    public bool Enabled { get; init; }
    
    public string CronExpression { get; init; } = "0 0/5 * * * ?";
    
    public bool RunSequentially { get; init; }

    public string IgnoredDownloadsPath { get; init; } = string.Empty;
    
    public ushort ImportFailedMaxStrikes { get; init; }
    
    public bool ImportFailedIgnorePrivate { get; init; }
    
    public bool ImportFailedDeletePrivate { get; init; }

    public IReadOnlyList<string> ImportFailedIgnorePatterns { get; init; } = [];
    
    public ushort StalledMaxStrikes { get; init; }
    
    public bool StalledResetStrikesOnProgress { get; init; }
    
    public bool StalledIgnorePrivate { get; init; }
    
    public bool StalledDeletePrivate { get; init; }
    
    public ushort DownloadingMetadataMaxStrikes { get; init; }
    
    public ushort SlowMaxStrikes { get; init; }
    
    public bool SlowResetStrikesOnProgress { get; init; }

    public bool SlowIgnorePrivate { get; init; }
    
    public bool SlowDeletePrivate { get; init; }

    public string SlowMinSpeed { get; init; } = string.Empty;
    
    [JsonIgnore]
    public ByteSize SlowMinSpeedByteSize => string.IsNullOrEmpty(SlowMinSpeed) ? new ByteSize(0) : ByteSize.Parse(SlowMinSpeed);
    
    public double SlowMaxTime { get; init; }
    
    public string SlowIgnoreAboveSize { get; init; } = string.Empty;
    
    [JsonIgnore]
    public ByteSize? SlowIgnoreAboveSizeByteSize => string.IsNullOrEmpty(SlowIgnoreAboveSize) ? null : ByteSize.Parse(SlowIgnoreAboveSize);
    
    public void Validate()
    {
        if (ImportFailedMaxStrikes is > 0 and < 3)
        {
            throw new ValidationException($"the minimum value for {SectionName.ToUpperInvariant()}__IMPORT_FAILED_MAX_STRIKES must be 3");
        }

        if (StalledMaxStrikes is > 0 and < 3)
        {
            throw new ValidationException($"the minimum value for {SectionName.ToUpperInvariant()}__STALLED_MAX_STRIKES must be 3");
        }
        
        if (DownloadingMetadataMaxStrikes is > 0 and < 3)
        {
            throw new ValidationException($"the minimum value for {SectionName.ToUpperInvariant()}__DOWNLOADING_METADATA_MAX_STRIKES must be 3");
        }
        
        if (SlowMaxStrikes is > 0 and < 3)
        {
            throw new ValidationException($"the minimum value for {SectionName.ToUpperInvariant()}__SLOW_MAX_STRIKES must be 3");
        }

        if (SlowMaxStrikes > 0)
        {
            bool isSlowSpeedSet = !string.IsNullOrEmpty(SlowMinSpeed);

            if (isSlowSpeedSet && ByteSize.TryParse(SlowMinSpeed, out _) is false)
            {
                throw new ValidationException($"invalid value for {SectionName.ToUpperInvariant()}__SLOW_MIN_SPEED");
            }

            if (SlowMaxTime < 0)
            {
                throw new ValidationException($"invalid value for {SectionName.ToUpperInvariant()}__SLOW_MAX_TIME");
            }

            if (!isSlowSpeedSet && SlowMaxTime is 0)
            {
                throw new ValidationException($"either {SectionName.ToUpperInvariant()}__SLOW_MIN_SPEED or {SectionName.ToUpperInvariant()}__SLOW_MAX_STRIKES must be set");
            }
        
            bool isSlowIgnoreAboveSizeSet = !string.IsNullOrEmpty(SlowIgnoreAboveSize);
        
            if (isSlowIgnoreAboveSizeSet && ByteSize.TryParse(SlowIgnoreAboveSize, out _) is false)
            {
                throw new ValidationException($"invalid value for {SectionName.ToUpperInvariant()}__SLOW_IGNORE_ABOVE_SIZE");
            }
        }
    }
}