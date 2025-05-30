namespace Common.Configuration.DTOs.Arr;

/// <summary>
/// DTO for updating Sonarr configuration (includes sensitive data fields)
/// </summary>
public class SonarrConfigUpdateDto : SonarrConfigDto
{
    /// <summary>
    /// Instances with sensitive data for updating
    /// </summary>
    public new List<ArrInstanceUpdateDto> Instances { get; set; } = new();
}
