namespace Common.Configuration.DTOs.Arr;

/// <summary>
/// DTO for updating Arr instance information (includes sensitive data fields)
/// </summary>
public class ArrInstanceUpdateDto : ArrInstanceDto
{
    /// <summary>
    /// API Key for authentication (only included in update DTO)
    /// </summary>
    public string? ApiKey { get; set; }
}
