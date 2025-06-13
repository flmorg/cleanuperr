namespace Common.Configuration.Notification;

public abstract record NotificationConfig
{
    public bool OnFailedImportStrike { get; init; }
    
    public bool OnStalledStrike { get; init; }
    
    public bool OnSlowStrike { get; init; }
    
    public bool OnQueueItemDeleted { get; init; }
    
    public bool OnDownloadCleaned { get; init; }
    
    public bool OnCategoryChanged { get; init; }

    public bool IsEnabled =>
        OnFailedImportStrike ||
        OnStalledStrike ||
        OnSlowStrike ||
        OnQueueItemDeleted ||
        OnDownloadCleaned ||
        OnCategoryChanged;

    public abstract bool IsValid();
}