namespace Common.Configuration.DTOs.General;

/// <summary>
/// DTO for updating General configuration (includes sensitive data fields)
/// </summary>
public class GeneralConfigUpdateDto : GeneralConfigDto
{
    /// <summary>
    /// Encryption key used for sensitive data (only included in update DTO)
    /// </summary>
    public string? EncryptionKey { get; set; }
}
