namespace Cleanuparr.Persistence.Models.Configuration.Notification;

public sealed record NotifiarrConfig : NotificationConfig
{
    public string? ApiKey { get; init; }
    
    public string? ChannelId { get; init; }

    public override bool IsValid()
    {
        if (string.IsNullOrEmpty(ApiKey?.Trim()))
        {
            return false;
        }

        if (string.IsNullOrEmpty(ChannelId?.Trim()))
        {
            return false;
        }

        return true;
    }
}