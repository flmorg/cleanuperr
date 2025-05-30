namespace Common.Configuration.DTOs.Notification;

/// <summary>
/// DTO for updating Notifications configuration (includes sensitive data fields)
/// </summary>
public class NotificationsConfigUpdateDto : NotificationsConfigDto
{
    /// <summary>
    /// Notifiarr notification configuration with sensitive data
    /// </summary>
    public new NotifiarrConfigUpdateDto Notifiarr { get; set; } = new();
    
    /// <summary>
    /// Apprise notification configuration with sensitive data
    /// </summary>
    public new AppriseConfigUpdateDto Apprise { get; set; } = new();
}
