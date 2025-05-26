using Data.Enums;

namespace Infrastructure.Verticals.Notifications;

public interface INotificationPublisher
{
    Task NotifyStrike(StrikeType strikeType, int strikeCount);
    
    Task NotifyQueueItemDeleted(bool removeFromClient, DeleteReason reason);
    
    Task NotifyDownloadCleaned(double ratio, TimeSpan seedingTime, string categoryName, CleanReason reason);

    Task NotifyCategoryChanged(string oldCategory, string newCategory, bool isTag = false);
}