namespace Common.Configuration.DTOs.Notification;

/// <summary>
/// Base DTO for notification configuration
/// </summary>
public abstract class BaseNotificationConfigDto
{
    /// <summary>
    /// Whether to notify on failed import strikes
    /// </summary>
    public bool OnFailedImportStrike { get; set; }
    
    /// <summary>
    /// Whether to notify on stalled download strikes
    /// </summary>
    public bool OnStalledStrike { get; set; }
    
    /// <summary>
    /// Whether to notify on slow download strikes
    /// </summary>
    public bool OnSlowStrike { get; set; }
    
    /// <summary>
    /// Whether to notify on queue item deletion
    /// </summary>
    public bool OnQueueItemDeleted { get; set; }
    
    /// <summary>
    /// Whether to notify on download cleaning
    /// </summary>
    public bool OnDownloadCleaned { get; set; }
    
    /// <summary>
    /// Whether to notify on category changes
    /// </summary>
    public bool OnCategoryChanged { get; set; }

    /// <summary>
    /// Whether any notification option is enabled
    /// </summary>
    public bool IsEnabled =>
        OnFailedImportStrike ||
        OnStalledStrike ||
        OnSlowStrike ||
        OnQueueItemDeleted ||
        OnDownloadCleaned ||
        OnCategoryChanged;
}
