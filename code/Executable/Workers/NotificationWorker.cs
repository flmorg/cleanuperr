using System.Collections.Concurrent;
using Infrastructure.Verticals.Notifications;
using Infrastructure.Verticals.Notifications.Models;

namespace Executable.Workers;

public sealed class NotificationWorker : BackgroundService
{
    private readonly ILogger<NotificationWorker> _logger;
    private readonly ConcurrentQueue<INotification> _notifications;
    private readonly NotificationService _notificationService;
    
    public NotificationWorker(
        ILogger<NotificationWorker> logger,
        ConcurrentQueue<INotification> notifications,
        NotificationService notificationService
    )
    {
        _logger = logger;
        _notifications = notifications;
        _notificationService = notificationService;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_notifications.TryDequeue(out INotification? notification))
                {
                    await Task.Delay(100, stoppingToken);
                    continue;
                }

                switch (notification)
                {
                    case FailedImportStrikeNotification failedMessage:
                        await _notificationService.Notify(failedMessage);
                        continue;
                    case StalledStrikeNotification stalledMessage:
                        await _notificationService.Notify(stalledMessage);
                        continue;
                    case QueueItemDeleteNotification queueItemDeleteMessage:
                        await _notificationService.Notify(queueItemDeleteMessage);
                        continue;
                    default:
                        throw new NotImplementedException();
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "error while processing notifications");
            }
        }
    }
}