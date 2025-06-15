namespace Executable.DTOs;

/// <summary>
/// DTO for updating Radarr configuration basic settings (instances managed separately)
/// </summary>
public record UpdateRadarrConfigDto
{
    public bool Enabled { get; init; }
    
    public short FailedImportMaxStrikes { get; init; } = -1;
} 