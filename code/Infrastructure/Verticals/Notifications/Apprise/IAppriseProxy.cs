using Data.Models.Configuration.Notification;

namespace Infrastructure.Verticals.Notifications.Apprise;

public interface IAppriseProxy
{
    Task SendNotification(ApprisePayload payload, AppriseConfig config);
}