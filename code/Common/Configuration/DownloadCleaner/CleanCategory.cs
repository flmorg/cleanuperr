using Common.Exceptions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Common.Configuration.DownloadCleaner;

public sealed record CleanCategory : IConfig
{
    public required string Name { get; init; }
    
    // TODO add clean type (category, tag, etc) and make it scoped to a client and rename this to cleanobject or something

    /// <summary>
    /// Max ratio before removing a download.
    /// </summary>
    [JsonProperty("max_ratio")]
    public required double MaxRatio { get; init; } = -1;

    /// <summary>
    /// Min number of hours to seed before removing a download, if the ratio has been met.
    /// </summary>
    [JsonProperty("min_seed_time")]
    public required double MinSeedTime { get; init; }

    /// <summary>
    /// Number of hours to seed before removing a download.
    /// </summary>
    [JsonProperty("max_seed_time")]
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