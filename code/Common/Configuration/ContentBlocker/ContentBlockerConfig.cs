using Common.Configuration.Arr;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Common.Configuration.ContentBlocker;

public sealed record ContentBlockerConfig : IJobConfig
{
    public const string SectionName = "ContentBlocker";
    
    public bool Enabled { get; init; }
    
    // Trigger configuration
    [JsonProperty("cron_expression")]
    public string CronExpression { get; init; } = "0 */30 * ? * *"; // Default: every 30 minutes
    
    // Privacy settings
    [JsonProperty("ignore_private")]
    public bool IgnorePrivate { get; init; }
    
    [JsonProperty("delete_private")]
    public bool DeletePrivate { get; init; }

    // TODO
    [JsonProperty("IGNORED_DOWNLOADS_PATH")]
    public string? IgnoredDownloadsPath { get; init; }
    
    // Blocklist settings moved from ArrConfig
    [JsonProperty("sonarr")]
    public BlocklistSettings Sonarr { get; init; } = new();
    
    [JsonProperty("radarr")]
    public BlocklistSettings Radarr { get; init; } = new();
    
    [JsonProperty("lidarr")]
    public BlocklistSettings Lidarr { get; init; } = new();
    
    public void Validate()
    {
        // Validation could check for valid cron expression, paths, etc.
    }
}

public record BlocklistSettings
{
    public bool Enabled { get; init; }
    public BlocklistType Type { get; init; }
    public string? Path { get; init; }
}