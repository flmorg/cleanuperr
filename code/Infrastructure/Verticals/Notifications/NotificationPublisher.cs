using System.Collections.Concurrent;
using Common.Configuration.Arr;
using Domain.Enums;
using Domain.Models.Arr.Queue;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.Notifications.Models;
using Mapster;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.Notifications;

public sealed class NotificationPublisher
{
    private readonly ILogger<NotificationPublisher> _logger;
    private readonly ConcurrentQueue<INotification> _notifications;

    public NotificationPublisher(ILogger<NotificationPublisher> logger, ConcurrentQueue<INotification> notifications)
    {
        _logger = logger;
        _notifications = notifications;
    }
    
    public void Notify(INotification notification)
    {
        _notifications.Enqueue(notification);
    }

    public void NotifyStrike(StrikeType strikeType, int strikeCount)
    {
        try
        {
            QueueRecord record = GetRecordFromContext();
            InstanceType instanceType = GetInstanceTypeFromContext();
            Uri instanceUrl = GetInstanceUrlFromContext();
            Uri? imageUrl = GetImageFromContext(record, instanceType);

            Notification notification = new()
            {
                InstanceType = instanceType,
                InstanceUrl = instanceUrl,
                Hash = record.DownloadId.ToLowerInvariant(),
                Title = $"Strike received with reason: {strikeType}",
                Description = record.Title,
                Image = imageUrl,
                Fields = [new() { Title = "Strike count", Text = strikeCount.ToString() }]
            };
            
            switch (strikeType)
            {
                case StrikeType.Stalled:
                    Notify(notification.Adapt<StalledStrikeNotification>());
                    break;
                case StrikeType.ImportFailed:
                    Notify(notification.Adapt<FailedImportStrikeNotification>());
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "failed to notify strike");
        }
    }

    public void NotifyQueueItemDelete(bool removeFromClient, DeleteReason reason)
    {
        QueueRecord record = GetRecordFromContext();
        InstanceType instanceType = GetInstanceTypeFromContext();
        Uri instanceUrl = GetInstanceUrlFromContext();
        Uri? imageUrl = GetImageFromContext(record, instanceType);
        
        Notification notification = new()
        {
            InstanceType = instanceType,
            InstanceUrl = instanceUrl,
            Hash = record.DownloadId.ToLowerInvariant(),
            Title = $"Deleting item from queue with reason: {reason}",
            Description = record.Title,
            Image = imageUrl,
            Fields = [new() { Title = "Removed from download client?", Text = removeFromClient ? "Yes" : "No" }]
        };
        
        Notify(notification.Adapt<QueueItemDeleteNotification>());
    }

    private static QueueRecord GetRecordFromContext() =>
        ContextProvider.Get<QueueRecord>(nameof(QueueRecord)) ?? throw new Exception("failed to get record from context");
    
    private static InstanceType GetInstanceTypeFromContext() =>
        (InstanceType)(ContextProvider.Get<object>(nameof(InstanceType)) ??
                       throw new Exception("failed to get instance type from context"));

    private static Uri GetInstanceUrlFromContext() =>
        ContextProvider.Get<Uri>(nameof(ArrInstance) + nameof(ArrInstance.Url)) ??
        throw new Exception("failed to get instance url from context");

    private static Uri GetImageFromContext(QueueRecord record, InstanceType instanceType) =>
        instanceType switch
        {
            InstanceType.Sonarr => record.Series!.Images.FirstOrDefault(x => x.CoverType == "poster")?.RemoteUrl,
            InstanceType.Radarr => record.Movie!.Images.FirstOrDefault(x => x.CoverType == "poster")?.RemoteUrl,
            InstanceType.Lidarr => record.Album!.Images.FirstOrDefault(x => x.CoverType == "cover")?.Url,
            _ => throw new ArgumentOutOfRangeException(nameof(instanceType))
        } ?? throw new Exception("failed to get image url from context");
}