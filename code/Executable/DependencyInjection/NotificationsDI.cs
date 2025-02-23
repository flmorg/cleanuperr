using Infrastructure.Verticals.Notifications;
using Infrastructure.Verticals.Notifications.Notifiarr;

namespace Executable.DependencyInjection;

public static class NotificationsDI
{
    public static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration configuration) =>
        services
            .Configure<NotifiarrConfig>(configuration.GetSection(NotifiarrConfig.SectionName))
            .AddTransient<INotifiarrProxy, NotifiarrProxy>()
            .AddTransient<INotificationProvider, NotifiarrProvider>()
            .AddTransient<INotificationPublisher, NotificationPublisher>()
            .AddTransient<INotificationFactory, NotificationFactory>()
            .AddTransient<NotificationService>();
}