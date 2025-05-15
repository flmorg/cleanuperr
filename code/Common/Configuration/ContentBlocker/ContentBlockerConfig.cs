using Common.Configuration.Arr;
using Microsoft.Extensions.Configuration;

namespace Common.Configuration.ContentBlocker;

public sealed record ContentBlockerConfig : IJobConfig
{
    public const string SectionName = "ContentBlocker";
    
    public bool Enabled { get; init; }
    
    // Trigger configuration
    [ConfigurationKeyName("CRON_EXPRESSION")]
    public string CronExpression { get; init; } = "0 */30 * ? * *"; // Default: every 30 minutes
    
    // Privacy settings
    [ConfigurationKeyName("IGNORE_PRIVATE")]
    public bool IgnorePrivate { get; init; }
    
    [ConfigurationKeyName("DELETE_PRIVATE")]
    public bool DeletePrivate { get; init; }

    [ConfigurationKeyName("IGNORED_DOWNLOADS_PATH")]
    public string? IgnoredDownloadsPath { get; init; }
    
    // Blocklist settings moved from ArrConfig
    public BlocklistSettings Sonarr { get; init; } = new();
    public BlocklistSettings Radarr { get; init; } = new();
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