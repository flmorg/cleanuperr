namespace Cleanuparr.Application.Features.Arr.Dtos;

/// <summary>
/// DTO for updating Lidarr configuration basic settings (instances managed separately)
/// </summary>
public record UpdateLidarrConfigDto
{
    public bool Enabled { get; init; }
    
    public short FailedImportMaxStrikes { get; init; } = -1;
} 