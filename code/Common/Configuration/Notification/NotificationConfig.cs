using Microsoft.Extensions.Configuration;

namespace Common.Configuration.Notification;

public abstract record NotificationConfig
{
    [ConfigurationKeyName("ON_IMPORT_FAILED_STRIKE")]
    public bool OnImportFailedStrike { get; init; }
    
    [ConfigurationKeyName("ON_STALLED_STRIKE")]
    public bool OnStalledStrike { get; init; }
    
    [ConfigurationKeyName("ON_QUEUE_ITEM_DELETE")]
    public bool OnQueueItemDelete { get; init; }

    public bool IsEnabled => OnImportFailedStrike || OnStalledStrike || OnQueueItemDelete;

    public abstract bool IsValid();
}