using System.Text;
using Common.Configuration.Notification;
using Infrastructure.Configuration;
using Infrastructure.Verticals.Notifications.Models;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.Notifications.Apprise;

public sealed class AppriseProvider : NotificationProvider
{
    private readonly IAppriseProxy _proxy;
    
    public override AppriseConfig Config => _config.Apprise;
    
    public override string Name => "Apprise";
    
    public AppriseProvider(IConfigManager configManager, IAppriseProxy proxy)
        : base(configManager)
    {
        _proxy = proxy;
    }

    public override async Task OnFailedImportStrike(FailedImportStrikeNotification notification)
    {
        await _proxy.SendNotification(BuildPayload(notification, NotificationType.Warning), Config);
    }
    
    public override async Task OnStalledStrike(StalledStrikeNotification notification)
    {
        await _proxy.SendNotification(BuildPayload(notification, NotificationType.Warning), Config);
    }
    
    public override async Task OnSlowStrike(SlowStrikeNotification notification)
    {
        await _proxy.SendNotification(BuildPayload(notification, NotificationType.Warning), Config);
    }
    
    public override async Task OnQueueItemDeleted(QueueItemDeletedNotification notification)
    {
        await _proxy.SendNotification(BuildPayload(notification, NotificationType.Warning), Config);
    }

    public override async Task OnDownloadCleaned(DownloadCleanedNotification notification)
    {
        await _proxy.SendNotification(BuildPayload(notification, NotificationType.Warning), Config);
    }
    
    public override async Task OnCategoryChanged(CategoryChangedNotification notification)
    {
        await _proxy.SendNotification(BuildPayload(notification, NotificationType.Warning), Config);
    }
    
    private static ApprisePayload BuildPayload(ArrNotification notification, NotificationType notificationType)
    {
        StringBuilder body = new();
        body.AppendLine(notification.Description);
        body.AppendLine();
        body.AppendLine($"Instance type: {notification.InstanceType.ToString()}");
        body.AppendLine($"Url: {notification.InstanceUrl}");
        body.AppendLine($"Download hash: {notification.Hash}");

        foreach (NotificationField field in notification.Fields ?? [])
        {
            body.AppendLine($"{field.Title}: {field.Text}");
        }
        
        ApprisePayload payload = new()
        {
            Title = notification.Title,
            Body = body.ToString(),
            Type = notificationType.ToString().ToLowerInvariant(),
        };
        
        return payload;
    }
    
    private static ApprisePayload BuildPayload(Notification notification, NotificationType notificationType)
    {
        StringBuilder body = new();
        body.AppendLine(notification.Description);
        body.AppendLine();

        foreach (NotificationField field in notification.Fields ?? [])
        {
            body.AppendLine($"{field.Title}: {field.Text}");
        }
        
        ApprisePayload payload = new()
        {
            Title = notification.Title,
            Body = body.ToString(),
            Type = notificationType.ToString().ToLowerInvariant(),
        };
        
        return payload;
    }
}