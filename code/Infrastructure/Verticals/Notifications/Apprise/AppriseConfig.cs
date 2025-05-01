using Common.Configuration.Notification;

namespace Infrastructure.Verticals.Notifications.Apprise;

public sealed record AppriseConfig : NotificationConfig
{
    public const string SectionName = "Apprise";

    public Uri? Url { get; init; }
    
    public string? Key { get; init; }
    
    public override bool IsValid()
    {
        if (Url is null)
        {
            return false;
        }
        
        if (string.IsNullOrEmpty(Key?.Trim()))
        {
            return false;
        }

        return true;
    }
}