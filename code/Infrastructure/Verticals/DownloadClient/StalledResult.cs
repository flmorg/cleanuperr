using Domain.Enums;

namespace Infrastructure.Verticals.DownloadClient;

public sealed record StalledResult
{
    /// <summary>
    /// True if the download should be removed; otherwise false.
    /// </summary>
    public bool ShouldRemove { get; set; }
    
    public DeleteReason DeleteReason { get; set; }
    
    /// <summary>
    /// True if the download is private; otherwise false.
    /// </summary>
    public bool IsPrivate { get; set; }
}