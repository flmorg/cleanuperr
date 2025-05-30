namespace Common.Configuration.DTOs.Arr;

/// <summary>
/// DTO for updating Lidarr configuration (includes sensitive data fields)
/// </summary>
public class LidarrConfigUpdateDto : LidarrConfigDto
{
    /// <summary>
    /// Instances with sensitive data for updating
    /// </summary>
    public new List<ArrInstanceUpdateDto> Instances { get; set; } = new();
}
