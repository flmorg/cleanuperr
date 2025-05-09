﻿using Infrastructure.Verticals.Notifications;
using Infrastructure.Verticals.Notifications.Apprise;
using Infrastructure.Verticals.Notifications.Notifiarr;

namespace Executable.DependencyInjection;

public static class NotificationsDI
{
    public static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration configuration) =>
        services
            .Configure<NotifiarrConfig>(configuration.GetSection(NotifiarrConfig.SectionName))
            .Configure<AppriseConfig>(configuration.GetSection(AppriseConfig.SectionName))
            .AddTransient<INotifiarrProxy, NotifiarrProxy>()
            .AddTransient<INotificationProvider, NotifiarrProvider>()
            .AddTransient<IAppriseProxy, AppriseProxy>()
            .AddTransient<INotificationProvider, AppriseProvider>()
            .AddTransient<INotificationPublisher, NotificationPublisher>()
            .AddTransient<INotificationFactory, NotificationFactory>()
            .AddTransient<NotificationService>();
}