namespace Common.Configuration.DTOs.Notification;

/// <summary>
/// DTO for updating Apprise configuration (includes sensitive data fields)
/// </summary>
public class AppriseConfigUpdateDto : AppriseConfigDto
{
    /// <summary>
    /// API Key for Apprise authentication (only included in update DTO)
    /// </summary>
    public string? Key { get; set; }
}
