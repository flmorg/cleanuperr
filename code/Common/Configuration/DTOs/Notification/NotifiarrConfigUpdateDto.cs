namespace Common.Configuration.DTOs.Notification;

/// <summary>
/// DTO for updating Notifiarr configuration (includes sensitive data fields)
/// </summary>
public class NotifiarrConfigUpdateDto : NotifiarrConfigDto
{
    /// <summary>
    /// API Key for Notifiarr authentication (only included in update DTO)
    /// </summary>
    public string? ApiKey { get; set; }
}
