using Domain.Enums;

namespace Infrastructure.Verticals.DownloadClient;

public sealed record SeedingCheckResult
{
    public bool ShouldClean { get; set; }
    public CleanReason Reason { get; set; }    
}