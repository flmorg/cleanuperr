using System.Text.Json.Serialization;
using Common.CustomDataTypes;
using Common.Exceptions;

namespace Common.Configuration.QueueCleaner;

public sealed record QueueCleanerConfig : IJobConfig
{
    public const string SectionName = "QueueCleaner";
    
    public bool Enabled { get; init; }
    
    public string CronExpression { get; init; } = "0 0/5 * * * ?";
    
    public string IgnoredDownloadsPath { get; init; } = string.Empty;

    public FailedImportConfig FailedImport { get; init; } = new();
    
    public StalledConfig Stalled { get; init; } = new();
    
    public SlowConfig Slow { get; init; } = new();
    
    public ContentBlockerConfig ContentBlocker { get; init; } = new();
    
    public void Validate()
    {
        FailedImport.Validate(SectionName);
        Stalled.Validate(SectionName);
        Slow.Validate(SectionName);
        ContentBlocker.Validate();
    }
}

public sealed record FailedImportConfig
{
    public ushort MaxStrikes { get; init; }
    
    public bool IgnorePrivate { get; init; }
    
    public bool DeletePrivate { get; init; }

    public IReadOnlyList<string> IgnoredPatterns { get; init; } = [];
    
    public void Validate(string sectionName)
    {
        if (MaxStrikes is > 0 and < 3)
        {
            throw new ValidationException($"the minimum value for {sectionName.ToUpperInvariant()}__FAILED_IMPORT__MAX_STRIKES must be 3");
        }
    }
}

public sealed record StalledConfig
{
    public ushort MaxStrikes { get; init; }
    
    public bool ResetStrikesOnProgress { get; init; }
    
    public bool IgnorePrivate { get; init; }
    
    public bool DeletePrivate { get; init; }
    
    public ushort DownloadingMetadataMaxStrikes { get; init; }
    
    public void Validate(string sectionName)
    {
        if (MaxStrikes is > 0 and < 3)
        {
            throw new ValidationException($"the minimum value for {sectionName.ToUpperInvariant()}__STALLED__MAX_STRIKES must be 3");
        }
        
        if (DownloadingMetadataMaxStrikes is > 0 and < 3)
        {
            throw new ValidationException($"the minimum value for {sectionName.ToUpperInvariant()}__STALLED__DOWNLOADING_METADATA_MAX_STRIKES must be 3");
        }
    }
}

public sealed record SlowConfig
{
    public ushort MaxStrikes { get; init; }
    
    public bool ResetStrikesOnProgress { get; init; }

    public bool IgnorePrivate { get; init; }
    
    public bool DeletePrivate { get; init; }

    public string MinSpeed { get; init; } = string.Empty;
    
    [JsonIgnore]
    public ByteSize MinSpeedByteSize => string.IsNullOrEmpty(MinSpeed) ? new ByteSize(0) : ByteSize.Parse(MinSpeed);
    
    public double MaxTime { get; init; }
    
    public string IgnoreAboveSize { get; init; } = string.Empty;
    
    [JsonIgnore]
    public ByteSize? IgnoreAboveSizeByteSize => string.IsNullOrEmpty(IgnoreAboveSize) ? null : ByteSize.Parse(IgnoreAboveSize);
    
    public void Validate(string sectionName)
    {
        if (MaxStrikes is > 0 and < 3)
        {
            throw new ValidationException($"the minimum value for {sectionName.ToUpperInvariant()}__SLOW__MAX_STRIKES must be 3");
        }

        if (MaxStrikes > 0)
        {
            bool isSpeedSet = !string.IsNullOrEmpty(MinSpeed);

            if (isSpeedSet && ByteSize.TryParse(MinSpeed, out _) is false)
            {
                throw new ValidationException($"invalid value for {sectionName.ToUpperInvariant()}__SLOW__MIN_SPEED");
            }

            if (MaxTime < 0)
            {
                throw new ValidationException($"invalid value for {sectionName.ToUpperInvariant()}__SLOW__MAX_TIME");
            }

            if (!isSpeedSet && MaxTime is 0)
            {
                throw new ValidationException($"either {sectionName.ToUpperInvariant()}__SLOW__MIN_SPEED or {sectionName.ToUpperInvariant()}__SLOW__MAX_TIME must be set");
            }
        
            bool isIgnoreAboveSizeSet = !string.IsNullOrEmpty(IgnoreAboveSize);
        
            if (isIgnoreAboveSizeSet && ByteSize.TryParse(IgnoreAboveSize, out _) is false)
            {
                throw new ValidationException($"invalid value for {sectionName.ToUpperInvariant()}__SLOW__IGNORE_ABOVE_SIZE");
            }
        }
    }
}