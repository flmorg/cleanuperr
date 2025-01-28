namespace Infrastructure.Verticals.Notifications;

public interface INotificationFactory
{
    List<INotificationProvider> OnFailedImportStrikeEnabled();
    
    List<INotificationProvider> OnStalledStrikeEnabled();
    
    List<INotificationProvider> OnQueueItemDeleteEnabled();
}