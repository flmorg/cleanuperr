using Common.Configuration.QueueCleaner;

namespace Common.Configuration.DTOs.ContentBlocker;

/// <summary>
/// DTO for retrieving ContentBlocker configuration
/// </summary>
public class ContentBlockerConfigDto
{
    /// <summary>
    /// Whether the content blocker is enabled
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// Cron expression for scheduling
    /// </summary>
    public string CronExpression { get; set; } = "0 0/5 * * * ?";
    
    /// <summary>
    /// Whether to ignore private torrents
    /// </summary>
    public bool IgnorePrivate { get; set; }
    
    /// <summary>
    /// Whether to delete private torrents
    /// </summary>
    public bool DeletePrivate { get; set; }

    /// <summary>
    /// Sonarr blocklist settings
    /// </summary>
    public BlocklistSettingsDto Sonarr { get; set; } = new();
    
    /// <summary>
    /// Radarr blocklist settings
    /// </summary>
    public BlocklistSettingsDto Radarr { get; set; } = new();
    
    /// <summary>
    /// Lidarr blocklist settings
    /// </summary>
    public BlocklistSettingsDto Lidarr { get; set; } = new();
}

/// <summary>
/// DTO for blocklist settings
/// </summary>
public class BlocklistSettingsDto
{
    /// <summary>
    /// Whether the blocklist settings are enabled
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// Type of blocklist
    /// </summary>
    public BlocklistType Type { get; set; }
    
    /// <summary>
    /// Path to blocklist file or URL
    /// </summary>
    public string? Path { get; set; }
}
