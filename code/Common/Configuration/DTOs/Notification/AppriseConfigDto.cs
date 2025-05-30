namespace Common.Configuration.DTOs.Notification;

/// <summary>
/// DTO for retrieving Apprise configuration (excludes sensitive data)
/// </summary>
public class AppriseConfigDto : BaseNotificationConfigDto
{
    /// <summary>
    /// URL of the Apprise API server
    /// </summary>
    public Uri? Url { get; set; }
    
    // Key is intentionally excluded for security
}
