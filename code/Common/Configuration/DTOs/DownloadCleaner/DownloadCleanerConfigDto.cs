namespace Common.Configuration.DTOs.DownloadCleaner;

/// <summary>
/// DTO for retrieving DownloadCleaner configuration
/// </summary>
public class DownloadCleanerConfigDto
{
    /// <summary>
    /// Whether the download cleaner is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Cron expression for scheduling
    /// </summary>
    public string CronExpression { get; set; } = "0 0 * * * ?";

    /// <summary>
    /// Categories for cleaning downloads
    /// </summary>
    public List<CleanCategoryDto> Categories { get; set; } = new();

    /// <summary>
    /// Whether to delete private torrents
    /// </summary>
    public bool DeletePrivate { get; set; }
    
    /// <summary>
    /// Path to ignored downloads file
    /// </summary>
    public string IgnoredDownloadsPath { get; set; } = string.Empty;

    /// <summary>
    /// Target category for unlinked downloads
    /// </summary>
    public string UnlinkedTargetCategory { get; set; } = "cleanuparr-unlinked";

    /// <summary>
    /// Whether to use tags instead of categories for unlinked downloads
    /// </summary>
    public bool UnlinkedUseTag { get; set; }

    /// <summary>
    /// Root directory to ignore for unlinked downloads
    /// </summary>
    public string UnlinkedIgnoredRootDir { get; set; } = string.Empty;
    
    /// <summary>
    /// Categories to consider as unlinked
    /// </summary>
    public List<string> UnlinkedCategories { get; set; } = new();
}
