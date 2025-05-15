using Microsoft.Extensions.Configuration;

namespace Common.Configuration.IgnoredDownloads;

/// <summary>
/// Configuration for ignored downloads
/// </summary>
public class IgnoredDownloadsConfig
{
    /// <summary>
    /// List of download IDs to be ignored by all jobs
    /// </summary>
    [ConfigurationKeyName("IGNORED_DOWNLOADS")]
    public List<string> IgnoredDownloads { get; init; } = new();
}
