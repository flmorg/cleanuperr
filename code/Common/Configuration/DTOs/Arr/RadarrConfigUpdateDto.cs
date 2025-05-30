namespace Common.Configuration.DTOs.Arr;

/// <summary>
/// DTO for updating Radarr configuration (includes sensitive data fields)
/// </summary>
public class RadarrConfigUpdateDto : RadarrConfigDto
{
    /// <summary>
    /// Instances with sensitive data for updating
    /// </summary>
    public new List<ArrInstanceUpdateDto> Instances { get; set; } = new();
}
