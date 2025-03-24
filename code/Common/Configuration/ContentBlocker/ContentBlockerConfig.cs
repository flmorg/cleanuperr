using Microsoft.Extensions.Configuration;

namespace Common.Configuration.ContentBlocker;

public sealed record ContentBlockerConfig : IJobConfig, IIgnoredDownloadsConfig
{
    public const string SectionName = "ContentBlocker";
    
    public required bool Enabled { get; init; }
    
    [ConfigurationKeyName("IGNORE_PRIVATE")]
    public bool IgnorePrivate { get; init; }
    
    [ConfigurationKeyName("DELETE_PRIVATE")]
    public bool DeletePrivate { get; init; }

    [ConfigurationKeyName("IGNORED_DOWNLOADS_PATH")]
    public string? IgnoredDownloadsPath { get; init; }
    
    public void Validate()
    {
    }
}