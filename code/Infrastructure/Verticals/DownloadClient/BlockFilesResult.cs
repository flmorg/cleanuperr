namespace Infrastructure.Verticals.DownloadClient;

public sealed record BlockFilesResult
{
    /// <summary>
    /// True if the download should be removed; otherwise false.
    /// </summary>
    public bool ShouldRemove { get; set; }
    
    /// <summary>
    /// True if the download is private; otherwise false.
    /// </summary>
    public bool IsPrivate { get; set; }
    
    /// <summary>
    /// True if the download was found by the client; otherwise false.
    /// </summary>
    public bool Found { get; set; }
}