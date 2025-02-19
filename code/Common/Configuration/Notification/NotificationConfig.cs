using Microsoft.Extensions.Configuration;

namespace Common.Configuration.Notification;

public abstract record NotificationConfig
{
    [ConfigurationKeyName("ON_IMPORT_FAILED_STRIKE")]
    public bool OnImportFailedStrike { get; init; }
    
    [ConfigurationKeyName("ON_STALLED_STRIKE")]
    public bool OnStalledStrike { get; init; }
    
    [ConfigurationKeyName("ON_QUEUE_ITEM_DELETED")]
    public bool OnQueueItemDeleted { get; init; }
    
    [ConfigurationKeyName("ON_DOWNLOAD_CLEANED")]
    public bool OnDownloadCleaned { get; init; }
    
    [ConfigurationKeyName("ON_CATEGORY_CHANGED")]
    public bool OnCategoryChanged { get; init; }

    public bool IsEnabled =>
        OnImportFailedStrike ||
        OnStalledStrike ||
        OnQueueItemDeleted ||
        OnDownloadCleaned ||
        OnCategoryChanged;

    public abstract bool IsValid();
}