namespace Common.Configuration.DTOs.Notification;

/// <summary>
/// DTO for retrieving Notifications configuration (excludes sensitive data)
/// </summary>
public class NotificationsConfigDto
{
    /// <summary>
    /// Notifiarr notification configuration
    /// </summary>
    public NotifiarrConfigDto Notifiarr { get; set; } = new();
    
    /// <summary>
    /// Apprise notification configuration
    /// </summary>
    public AppriseConfigDto Apprise { get; set; } = new();
}
