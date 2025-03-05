using Common.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Common.Configuration.DownloadCleaner;

public sealed record DownloadCleanerConfig : IJobConfig, IIgnoredDownloadsConfig
{
    public const string SectionName = "DownloadCleaner";
    
    public bool Enabled { get; init; }
    
    public List<Category>? Categories { get; init; }

    [ConfigurationKeyName("DELETE_PRIVATE")]
    public bool DeletePrivate { get; init; }
    
    [ConfigurationKeyName("IGNORED_DOWNLOADS_PATH")]
    public string? IgnoredDownloadsPath { get; init; }

    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }
        
        if (Categories?.Count is null or 0)
        {
            throw new ValidationException("no categories configured");
        }

        if (Categories?.GroupBy(x => x.Name).Any(x => x.Count() > 1) is true)
        {
            throw new ValidationException("duplicated categories found");
        }
        
        Categories?.ForEach(x => x.Validate());
    }
}