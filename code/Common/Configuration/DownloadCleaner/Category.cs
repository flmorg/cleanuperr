using Microsoft.Extensions.Configuration;

namespace Common.Configuration.DownloadCleaner;

public sealed record Category : IConfig
{
    public required string Name { get; init; }
    
    /// <summary>
    /// Max ratio before removing a download.
    /// </summary>
    [ConfigurationKeyName("MAX_RATIO")]
    public required float MaxRatio { get; init; }
    
    /// <summary>
    /// Min number of hours to seed before removing a download, if the ratio has been met.
    /// </summary>
    [ConfigurationKeyName("MIN_SEED_TIME")]
    public required float MinSeedTime { get; init; }
    
    /// <summary>
    /// Number of hours to seed before removing a download.
    /// </summary>
    [ConfigurationKeyName("MAX_SEED_TIME")]
    public required float MaxSeedTime { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("can not be empty", nameof(Name));
        }

        if (MaxRatio < 0)
        {
            throw new ArgumentException("can not be negative", nameof(MaxRatio));
        }

        if (MaxSeedTime > 0 && MinSeedTime > 0 && MaxSeedTime < MinSeedTime)
        {
            throw new ArgumentException("can not be less than min seed time", nameof(MaxSeedTime));
        }
    }
}