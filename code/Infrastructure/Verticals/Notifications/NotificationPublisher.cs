using System.Globalization;
using Common.Attributes;
using Common.Configuration.Arr;
using Domain.Enums;
using Domain.Models.Arr.Queue;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.Notifications.Models;
using Mapster;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.Notifications;

public class NotificationPublisher : InterceptedService, IDryRunService
{
    private readonly ILogger<NotificationPublisher> _logger;
    private readonly IBus _messageBus;
    
    /// <summary>
    /// Constructor to be used by interceptors.
    /// </summary>
    public NotificationPublisher()
    {
    }
    
    public NotificationPublisher(ILogger<NotificationPublisher> logger, IBus messageBus)
    {
        _logger = logger;
        _messageBus = messageBus;
    }
    
    [DryRunSafeguard]
    public virtual async Task NotifyStrike(StrikeType strikeType, int strikeCount)
    {
        try
        {
            QueueRecord record = ContextProvider.Get<QueueRecord>(nameof(QueueRecord));
            InstanceType instanceType = (InstanceType)ContextProvider.Get<object>(nameof(InstanceType));
            Uri instanceUrl = ContextProvider.Get<Uri>(nameof(ArrInstance) + nameof(ArrInstance.Url));
            Uri imageUrl = GetImageFromContext(record, instanceType);

            ArrNotification notification = new()
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
                    await _messageBus.Publish(notification.Adapt<StalledStrikeNotification>());
                    break;
                case StrikeType.ImportFailed:
                    await _messageBus.Publish(notification.Adapt<FailedImportStrikeNotification>());
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "failed to notify strike");
        }
    }

    [DryRunSafeguard]
    public virtual async Task NotifyQueueItemDeleted(bool removeFromClient, DeleteReason reason)
    {
        QueueRecord record = ContextProvider.Get<QueueRecord>(nameof(QueueRecord));
        InstanceType instanceType = (InstanceType)ContextProvider.Get<object>(nameof(InstanceType));
        Uri instanceUrl = ContextProvider.Get<Uri>(nameof(ArrInstance) + nameof(ArrInstance.Url));
        Uri imageUrl = GetImageFromContext(record, instanceType);
        
        QueueItemDeletedNotification notification = new()
        {
            InstanceType = instanceType,
            InstanceUrl = instanceUrl,
            Hash = record.DownloadId.ToLowerInvariant(),
            Title = $"Deleting item from queue with reason: {reason}",
            Description = record.Title,
            Image = imageUrl,
            Fields = [new() { Title = "Removed from download client?", Text = removeFromClient ? "Yes" : "No" }]
        };
        
        await _messageBus.Publish(notification);
    }

    [DryRunSafeguard]
    public virtual async Task NotifyDownloadCleaned(double ratio, TimeSpan seedingTime, string categoryName, CleanReason reason)
    {
        DownloadCleanedNotification notification = new()
        {
            Title = $"Cleaned item from download client with reason: {reason}",
            Description = ContextProvider.Get<string>("downloadName"),
            Fields =
            [
                new() { Title = "Hash", Text = ContextProvider.Get<string>("hash").ToLowerInvariant() },
                new() { Title = "Category", Text = categoryName.ToLowerInvariant() },
                new() { Title = "Ratio", Text = $"{ratio.ToString(CultureInfo.InvariantCulture)}%" },
                new() { Title = "Seeding hours", Text = $"{Math.Round(seedingTime.TotalHours, 0).ToString(CultureInfo.InvariantCulture)}h" }
            ],
            Level = NotificationLevel.Important
        };

        await _messageBus.Publish(notification);
    }
    
    private static Uri GetImageFromContext(QueueRecord record, InstanceType instanceType) =>
        instanceType switch
        {
            InstanceType.Sonarr => record.Series!.Images.FirstOrDefault(x => x.CoverType == "poster")?.RemoteUrl,
            InstanceType.Radarr => record.Movie!.Images.FirstOrDefault(x => x.CoverType == "poster")?.RemoteUrl,
            InstanceType.Lidarr => record.Album!.Images.FirstOrDefault(x => x.CoverType == "cover")?.Url,
            _ => throw new ArgumentOutOfRangeException(nameof(instanceType))
        } ?? throw new Exception("failed to get image url from context");
}