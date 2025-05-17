namespace Common.Configuration.Notification;

public sealed record NotificationsConfig
{
    public NotifiarrConfig Notifiarr { get; init; } = new();
    
    public AppriseConfig Apprise { get; init; } = new();
}