namespace Cleanuparr.Infrastructure.Features.Notifications;

public class NotificationFactory : INotificationFactory
{
    private readonly IEnumerable<INotificationProvider> _providers;
    
    public NotificationFactory(IEnumerable<INotificationProvider> providers)
    {
        _providers = providers;
    }
    
    protected List<INotificationProvider> ActiveProviders() =>
        _providers
            .Where(x => x.Config.IsValid())
            .Where(provider => provider.Config.IsEnabled)
            .ToList();

    public List<INotificationProvider> OnFailedImportStrikeEnabled() =>
        ActiveProviders()
            .Where(n => n.Config.OnFailedImportStrike)
            .ToList();

    public List<INotificationProvider> OnStalledStrikeEnabled() =>
        ActiveProviders()
            .Where(n => n.Config.OnStalledStrike)
            .ToList();
    
    public List<INotificationProvider> OnSlowStrikeEnabled() =>
        ActiveProviders()
            .Where(n => n.Config.OnSlowStrike)
            .ToList();

    public List<INotificationProvider> OnQueueItemDeletedEnabled() =>
        ActiveProviders()
            .Where(n => n.Config.OnQueueItemDeleted)
            .ToList();
    
    public List<INotificationProvider> OnDownloadCleanedEnabled() =>
        ActiveProviders()
            .Where(n => n.Config.OnDownloadCleaned)
            .ToList();
    
    public List<INotificationProvider> OnCategoryChangedEnabled() =>
        ActiveProviders()
            .Where(n => n.Config.OnCategoryChanged)
            .ToList();
}