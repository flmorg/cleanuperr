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

    public async Task Notify(Notification notification)
    {
        foreach (INotificationProvider provider in _notificationFactory.OnFailedImportStrikeEnabled())
        {
            try
            {
                switch (notification)
                {
                    case FailedImportStrikeNotification failedMessage:
                        await provider.OnFailedImportStrike(failedMessage);
                        break;
                    case StalledStrikeNotification stalledMessage:
                        await provider.OnStalledStrike(stalledMessage);
                        break;
                    case QueueItemDeleteNotification queueItemDeleteMessage:
                        await provider.OnQueueItemDelete(queueItemDeleteMessage);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "failed to send notification | provider {provider}", provider.Name);
            }
        }
    }
}