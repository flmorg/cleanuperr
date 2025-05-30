namespace Common.Configuration.DTOs.IgnoredDownloads;

/// <summary>
/// DTO for retrieving IgnoredDownloads configuration
/// </summary>
public class IgnoredDownloadsConfigDto
{
    /// <summary>
    /// List of download IDs to be ignored by all jobs
    /// </summary>
    public List<string> IgnoredDownloads { get; set; } = new();
}
