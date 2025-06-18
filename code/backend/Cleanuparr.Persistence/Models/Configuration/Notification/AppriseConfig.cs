namespace Cleanuparr.Persistence.Models.Configuration.Notification;

public sealed record AppriseConfig : NotificationConfig
{
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