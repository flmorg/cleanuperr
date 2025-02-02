namespace Infrastructure.Verticals.Notifications.Notifiarr;

public interface INotifiarrProxy
{
    Task SendNotification(NotifiarrPayload payload, NotifiarrConfig config);
}