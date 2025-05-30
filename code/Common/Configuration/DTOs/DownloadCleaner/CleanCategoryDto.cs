using Newtonsoft.Json;

namespace Common.Configuration.DTOs.DownloadCleaner;

/// <summary>
/// DTO for clean category configuration
/// </summary>
public class CleanCategoryDto
{
    /// <summary>
    /// Name of the clean category
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Max ratio before removing a download
    /// </summary>
    [JsonProperty("max_ratio")]
    public double MaxRatio { get; set; } = -1;

    /// <summary>
    /// Min number of hours to seed before removing a download, if the ratio has been met
    /// </summary>
    [JsonProperty("min_seed_time")]
    public double MinSeedTime { get; set; }

    /// <summary>
    /// Number of hours to seed before removing a download
    /// </summary>
    [JsonProperty("max_seed_time")]
    public double MaxSeedTime { get; set; } = -1;
}
