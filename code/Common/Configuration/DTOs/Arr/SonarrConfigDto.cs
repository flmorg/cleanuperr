using Common.Configuration.Arr;

namespace Common.Configuration.DTOs.Arr;

/// <summary>
/// DTO for retrieving Sonarr configuration (excludes sensitive data)
/// </summary>
public class SonarrConfigDto : ArrConfigDto
{
    /// <summary>
    /// Type of search used by Sonarr
    /// </summary>
    public SonarrSearchType SearchType { get; set; } = SonarrSearchType.Episode;
}
