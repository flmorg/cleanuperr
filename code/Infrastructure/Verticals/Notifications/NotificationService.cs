using Infrastructure.Verticals.Notifications.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.Notifications;

public class NotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly INotificationFactory _notificationFactory;

    public NotificationService(ILogger<NotificationService> logger, INotificationFactory notificationFactory)
    {
        _logger = logger;
        _notificationFactory = notificationFactory;
    }

    public async Task Notify(FailedImportStrikeNotification notification)
    {
        foreach (INotificationProvider provider in _notificationFactory.OnFailedImportStrikeEnabled())
        {
            try
            {
                await provider.OnFailedImportStrike(notification);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "failed to send notification | provider {provider}", provider.Name);
            }
        }
    }
    
    public async Task Notify(StalledStrikeNotification notification)
    {
        foreach (INotificationProvider provider in _notificationFactory.OnStalledStrikeEnabled())
        {
            try
            {
                await provider.OnStalledStrike(notification);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "failed to send notification | provider {provider}", provider.Name);
            }
        }
    }
    
    public async Task Notify(QueueItemDeleteNotification notification)
    {
        foreach (INotificationProvider provider in _notificationFactory.OnQueueItemDeleteEnabled())
        {
            try
            {
                await provider.OnQueueItemDelete(notification);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "failed to send notification | provider {provider}", provider.Name);
            }
        }
    }
}