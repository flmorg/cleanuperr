using Infrastructure.Verticals.Notifications;
using Infrastructure.Verticals.Notifications.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Infrastructure.Events;

/// <summary>
/// Wrapper around NotificationService that publishes events for all notification calls
/// </summary>
public class NotificationEventWrapper
{
    private readonly NotificationService _notificationService;
    private readonly EventPublisher _eventPublisher;
    private readonly ILogger<NotificationEventWrapper> _logger;

    public NotificationEventWrapper(
        NotificationService notificationService,
        EventPublisher eventPublisher,
        ILogger<NotificationEventWrapper> logger)
    {
        _notificationService = notificationService;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Notify(FailedImportStrikeNotification notification)
    {
        await NotifyWithEventLogging("FailedImportStrike", notification, 
            async () => await _notificationService.Notify(notification));
    }

    public async Task Notify(StalledStrikeNotification notification)
    {
        await NotifyWithEventLogging("StalledStrike", notification,
            async () => await _notificationService.Notify(notification));
    }

    public async Task Notify(SlowStrikeNotification notification)
    {
        await NotifyWithEventLogging("SlowStrike", notification,
            async () => await _notificationService.Notify(notification));
    }

    public async Task Notify(QueueItemDeletedNotification notification)
    {
        await NotifyWithEventLogging("QueueItemDeleted", notification,
            async () => await _notificationService.Notify(notification));
    }

    public async Task Notify(DownloadCleanedNotification notification)
    {
        await NotifyWithEventLogging("DownloadCleaned", notification,
            async () => await _notificationService.Notify(notification));
    }

    public async Task Notify(CategoryChangedNotification notification)
    {
        await NotifyWithEventLogging("CategoryChanged", notification,
            async () => await _notificationService.Notify(notification));
    }

    private async Task NotifyWithEventLogging<T>(string notificationType, T notification, Func<Task> notifyAction)
        where T : class
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Log notification attempt
            await _eventPublisher.PublishInfoAsync(
                source: "NotificationService",
                message: $"Sending {notificationType} notification",
                data: new { NotificationType = notificationType, Notification = notification },
                correlationId: correlationId);

            // Execute the notification
            await notifyAction();

            stopwatch.Stop();

            // Log successful notification
            await _eventPublisher.PublishInfoAsync(
                source: "NotificationService",
                message: $"{notificationType} notification sent successfully",
                data: new { 
                    NotificationType = notificationType, 
                    Duration = stopwatch.ElapsedMilliseconds,
                    Success = true 
                },
                correlationId: correlationId);

            _logger.LogInformation("Successfully sent {notificationType} notification in {duration}ms", 
                notificationType, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log failed notification
            await _eventPublisher.PublishErrorAsync(
                source: "NotificationService",
                message: $"Failed to send {notificationType} notification: {ex.Message}",
                data: new { 
                    NotificationType = notificationType,
                    Duration = stopwatch.ElapsedMilliseconds,
                    Success = false,
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                },
                correlationId: correlationId);

            _logger.LogError(ex, "Failed to send {notificationType} notification after {duration}ms", 
                notificationType, stopwatch.ElapsedMilliseconds);

            throw; // Re-throw to maintain original behavior
        }
    }
} 