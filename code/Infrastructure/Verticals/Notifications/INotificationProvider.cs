using Common.Configuration.Notification;
using Infrastructure.Verticals.Notifications.Models;

namespace Infrastructure.Verticals.Notifications;

public interface INotificationProvider
{
    NotificationConfig Config { get; }
    
    string Name { get; }
    
    Task OnFailedImportStrike(FailedImportStrikeNotification notification);
        
    Task OnStalledStrike(StalledStrikeNotification notification);

    Task OnQueueItemDeleted(QueueItemDeletedNotification notification);

    Task OnDownloadCleaned(DownloadCleanedNotification notification);
}