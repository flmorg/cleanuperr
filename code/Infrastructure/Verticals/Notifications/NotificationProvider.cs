using Common.Configuration.Notification;
using Infrastructure.Configuration;
using Infrastructure.Verticals.Notifications.Models;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.Notifications;

public abstract class NotificationProvider : INotificationProvider
{
    private readonly IConfigManager _configManager;
    protected readonly NotificationsConfig _config;
    
    public abstract BaseNotificationConfig Config { get; }

    protected NotificationProvider(IConfigManager configManager)
    {
        _configManager = configManager;
        _config = configManager.GetNotificationsConfig();
    }
    
    public abstract string Name { get; }
    
    public abstract Task OnFailedImportStrike(FailedImportStrikeNotification notification);

    public abstract Task OnStalledStrike(StalledStrikeNotification notification);
    
    public abstract Task OnSlowStrike(SlowStrikeNotification notification);

    public abstract Task OnQueueItemDeleted(QueueItemDeletedNotification notification);

    public abstract Task OnDownloadCleaned(DownloadCleanedNotification notification);
    
    public abstract Task OnCategoryChanged(CategoryChangedNotification notification);
}