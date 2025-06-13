namespace Common.Configuration.DTOs.Arr;

/// <summary>
/// DTO for Arr instance information (excludes sensitive data)
/// </summary>
public class ArrInstanceDto
{
    /// <summary>
    /// Unique identifier for this instance
    /// </summary>
    public Guid? Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Friendly name for this instance
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// URL of the instance
    /// </summary>
    public required Uri Url { get; set; }
    
    // ApiKey is intentionally excluded for security
}
