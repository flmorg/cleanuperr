using Common.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Common.Configuration.DownloadCleaner;

public sealed record Category : IConfig
{
    public required string Name { get; init; }

    /// <summary>
    /// Max ratio before removing a download.
    /// </summary>
    [ConfigurationKeyName("MAX_RATIO")]
    public required double MaxRatio { get; init; } = -1;

    /// <summary>
    /// Min number of hours to seed before removing a download, if the ratio has been met.
    /// </summary>
    [ConfigurationKeyName("MIN_SEED_TIME")]
    public required double MinSeedTime { get; init; } = 0;

    /// <summary>
    /// Number of hours to seed before removing a download.
    /// </summary>
    [ConfigurationKeyName("MAX_SEED_TIME")]
    public required double MaxSeedTime { get; init; } = -1;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ValidationException($"{nameof(Name)} can not be empty");
        }

        if (MaxRatio < 0 && MaxSeedTime < 0)
        {
            throw new ValidationException($"both {nameof(MaxRatio)} and {nameof(MaxSeedTime)} are disabled");
        }

        if (MinSeedTime < 0)
        {
            throw new ValidationException($"{nameof(MinSeedTime)} can not be negative");
        }
    }
}