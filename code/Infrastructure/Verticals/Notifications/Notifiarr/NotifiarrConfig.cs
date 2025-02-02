using Common.Configuration.Notification;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Verticals.Notifications.Notifiarr;

public sealed record NotifiarrConfig : NotificationConfig
{
    public const string SectionName = "Notifiarr";
    
    [ConfigurationKeyName("API_KEY")]
    public string? ApiKey { get; init; }
    
    [ConfigurationKeyName("CHANNEL_ID")]
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