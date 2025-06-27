using Cleanuparr.Persistence.Models.Configuration.Notification;
using Infrastructure.Verticals.Notifications.Models;
using Microsoft.EntityFrameworkCore;

namespace Cleanuparr.Infrastructure.Features.Notifications;

public abstract class NotificationProvider<T> : INotificationProvider<T>
    where T : NotificationConfig
{
    protected readonly DbSet<T> _notificationConfig;
    protected T? _config;
    
    public T Config => _config ??= _notificationConfig.First();
    
    NotificationConfig INotificationProvider.Config => Config;

    protected NotificationProvider(DbSet<T> notificationConfig)
    {
        _notificationConfig = notificationConfig;
    }

    public abstract string Name { get; }
    
    public abstract Task OnFailedImportStrike(FailedImportStrikeNotification notification);

    public abstract Task OnStalledStrike(StalledStrikeNotification notification);
    
    public abstract Task OnSlowStrike(SlowStrikeNotification notification);

    public abstract Task OnQueueItemDeleted(QueueItemDeletedNotification notification);

    public abstract Task OnDownloadCleaned(DownloadCleanedNotification notification);
    
    public abstract Task OnCategoryChanged(CategoryChangedNotification notification);
}