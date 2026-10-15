using Cleanuparr.Infrastructure.Features.Notifications.Models;

namespace Cleanuparr.Api.Features.Notifications.Contracts.Requests;

public abstract record UpdateNotificationProviderRequestBase : NotificationEventFlags
{
    public string Name { get; init; } = string.Empty;

    public bool IsEnabled { get; init; }
}
