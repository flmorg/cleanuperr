using System.Collections.Concurrent;
using Executable.Workers;
using Infrastructure.Verticals.Notifications;
using Infrastructure.Verticals.Notifications.Notifiarr;

namespace Executable.DependencyInjection;

public static class NotificationsDI
{
    public static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration configuration) =>
        services
            .Configure<NotifiarrConfig>(configuration.GetSection(NotifiarrConfig.SectionName))
            .AddSingleton<ConcurrentQueue<INotification>>()
            .AddTransient<INotifiarrProxy, NotifiarrProxy>()
            .AddTransient<INotificationProvider, NotifiarrProvider>()
            .AddTransient<NotificationPublisher>()
            .AddTransient<INotificationFactory, NotificationFactory>()
            .AddTransient<NotificationService>()
            .AddHostedService<NotificationWorker>();
}