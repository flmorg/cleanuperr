using Common.Configuration.Notification;
using Infrastructure.Configuration;
using Infrastructure.Verticals.Notifications.Models;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.Notifications;

public abstract class NotificationProvider : INotificationProvider
{
    protected NotificationProvider(IConfigManager configManager)
    {
        ConfigManager = configManager;
        Config = configManager.GetConfiguration<NotificationConfig>("notification.json") ?? new NotificationConfig();
    }
    
    public abstract string Name { get; }
    
    public IConfigManager ConfigManager { get; }
    
    public NotificationConfig Config { get; }
    
    public abstract Task OnFailedImportStrike(FailedImportStrikeNotification notification);

    public abstract Task OnStalledStrike(StalledStrikeNotification notification);
    
    public abstract Task OnSlowStrike(SlowStrikeNotification notification);

    public abstract Task OnQueueItemDeleted(QueueItemDeletedNotification notification);

    public abstract Task OnDownloadCleaned(DownloadCleanedNotification notification);
    
    public abstract Task OnCategoryChanged(CategoryChangedNotification notification);
}