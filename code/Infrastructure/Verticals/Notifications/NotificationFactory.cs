namespace Infrastructure.Verticals.Notifications;

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
            .Where(n => n.Config.OnImportFailedStrike)
            .ToList();

    public List<INotificationProvider> OnStalledStrikeEnabled() =>
        ActiveProviders()
            .Where(n => n.Config.OnStalledStrike)
            .ToList();

    public List<INotificationProvider> OnQueueItemDeleteEnabled() =>
        ActiveProviders()
            .Where(n => n.Config.OnQueueItemDelete)
            .ToList();
}