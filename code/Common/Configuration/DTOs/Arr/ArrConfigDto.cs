namespace Common.Configuration.DTOs.Arr;

/// <summary>
/// Base DTO for Arr configurations (excludes sensitive data)
/// </summary>
public abstract class ArrConfigDto
{
    /// <summary>
    /// Whether this configuration is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Maximum number of strikes for failed imports (-1 for unlimited)
    /// </summary>
    public short FailedImportMaxStrikes { get; set; } = -1;
    
    /// <summary>
    /// Instances of the Arr application
    /// </summary>
    public List<ArrInstanceDto> Instances { get; set; } = new();
}
