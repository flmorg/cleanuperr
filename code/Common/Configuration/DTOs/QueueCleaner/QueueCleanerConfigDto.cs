using Common.Configuration.QueueCleaner;

namespace Common.Configuration.DTOs.QueueCleaner;

/// <summary>
/// DTO for retrieving QueueCleaner configuration
/// </summary>
public class QueueCleanerConfigDto
{
    /// <summary>
    /// Whether the queue cleaner is enabled
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// Cron expression for scheduling
    /// </summary>
    public string CronExpression { get; set; } = "0 0/5 * * * ?";
    
    /// <summary>
    /// Whether to run jobs sequentially
    /// </summary>
    public bool RunSequentially { get; set; }

    /// <summary>
    /// Path to ignored downloads file
    /// </summary>
    public string IgnoredDownloadsPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Maximum number of strikes for failed imports
    /// </summary>
    public ushort FailedImportMaxStrikes { get; set; }
    
    /// <summary>
    /// Whether to ignore private torrents for failed imports
    /// </summary>
    public bool FailedImportIgnorePrivate { get; set; }
    
    /// <summary>
    /// Whether to delete private torrents for failed imports
    /// </summary>
    public bool FailedImportDeletePrivate { get; set; }

    /// <summary>
    /// Patterns to ignore for failed imports
    /// </summary>
    public IReadOnlyList<string> FailedImportIgnorePatterns { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Maximum number of strikes for stalled downloads
    /// </summary>
    public ushort StalledMaxStrikes { get; set; }
    
    /// <summary>
    /// Whether to reset strikes on progress for stalled downloads
    /// </summary>
    public bool StalledResetStrikesOnProgress { get; set; }
    
    /// <summary>
    /// Whether to ignore private torrents for stalled downloads
    /// </summary>
    public bool StalledIgnorePrivate { get; set; }
    
    /// <summary>
    /// Whether to delete private torrents for stalled downloads
    /// </summary>
    public bool StalledDeletePrivate { get; set; }
    
    /// <summary>
    /// Maximum number of strikes for downloading metadata
    /// </summary>
    public ushort DownloadingMetadataMaxStrikes { get; set; }
    
    /// <summary>
    /// Maximum number of strikes for slow downloads
    /// </summary>
    public ushort SlowMaxStrikes { get; set; }
    
    /// <summary>
    /// Whether to reset strikes on progress for slow downloads
    /// </summary>
    public bool SlowResetStrikesOnProgress { get; set; }

    /// <summary>
    /// Whether to ignore private torrents for slow downloads
    /// </summary>
    public bool SlowIgnorePrivate { get; set; }
    
    /// <summary>
    /// Whether to delete private torrents for slow downloads
    /// </summary>
    public bool SlowDeletePrivate { get; set; }

    /// <summary>
    /// Minimum speed threshold for slow downloads
    /// </summary>
    public string SlowMinSpeed { get; set; } = string.Empty;
    
    /// <summary>
    /// Maximum time allowed for slow downloads
    /// </summary>
    public double SlowMaxTime { get; set; }
    
    /// <summary>
    /// Size threshold above which slow downloads are ignored
    /// </summary>
    public string SlowIgnoreAboveSize { get; set; } = string.Empty;
}
