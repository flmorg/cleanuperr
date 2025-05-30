namespace Common.Configuration.DTOs.Notification;

/// <summary>
/// DTO for retrieving Notifiarr configuration (excludes sensitive data)
/// </summary>
public class NotifiarrConfigDto : BaseNotificationConfigDto
{
    /// <summary>
    /// Channel ID for Notifiarr notifications
    /// </summary>
    public string? ChannelId { get; set; }
    
    // ApiKey is intentionally excluded for security
}
