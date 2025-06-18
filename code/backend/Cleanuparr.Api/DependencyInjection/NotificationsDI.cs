using Cleanuparr.Infrastructure.Features.Notifications;
using Cleanuparr.Infrastructure.Features.Notifications.Apprise;
using Cleanuparr.Infrastructure.Features.Notifications.Notifiarr;
using Infrastructure.Verticals.Notifications;

namespace Cleanuparr.Api.DependencyInjection;

public static class NotificationsDI
{
    public static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration configuration) =>
        services
            // Notification configs are now managed through ConfigManager
            .AddTransient<INotifiarrProxy, NotifiarrProxy>()
            .AddTransient<INotificationProvider, NotifiarrProvider>()
            .AddTransient<IAppriseProxy, AppriseProxy>()
            .AddTransient<INotificationProvider, AppriseProvider>()
            .AddTransient<INotificationPublisher, NotificationPublisher>()
            .AddTransient<INotificationFactory, NotificationFactory>()
            .AddTransient<NotificationService>();
}