using Common.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Common.Configuration.DownloadCleaner;

public sealed record DownloadCleanerConfig : IJobConfig, IIgnoredDownloadsConfig
{
    public const string SectionName = "DownloadCleaner";
    
    public bool Enabled { get; init; }

    public List<CleanCategory>? Categories { get; init; }

    [ConfigurationKeyName("DELETE_PRIVATE")]
    public bool DeletePrivate { get; init; }
    
    [ConfigurationKeyName("IGNORED_DOWNLOADS_PATH")]
    public string? IgnoredDownloadsPath { get; init; }

    [ConfigurationKeyName("UNLINKED_TARGET_CATEGORY")]
    public string UnlinkedTargetCategory { get; init; } = "cleanuperr-unlinked";

    [ConfigurationKeyName("UNLINKED_IGNORED_ROOT_DIR")]
    public string UnlinkedIgnoredRootDir { get; init; } = string.Empty;
    
    [ConfigurationKeyName("UNLINKED_CATEGORIES")]
    public List<string>? UnlinkedCategories { get; init; }

    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }
        
        if (Categories?.GroupBy(x => x.Name).Any(x => x.Count() > 1) is true)
        {
            throw new ValidationException("duplicated clean categories found");
        }
        
        Categories?.ForEach(x => x.Validate());
        
        if (string.IsNullOrEmpty(UnlinkedTargetCategory))
        {
            return;
        }

        if (UnlinkedCategories?.Count is null or 0)
        {
            throw new ValidationException("no unlinked categories configured");
        }

        if (UnlinkedCategories.Contains(UnlinkedTargetCategory))
        {
            throw new ValidationException($"{SectionName.ToUpperInvariant()}__UNLINKED_TARGET_CATEGORY should not be present in {SectionName.ToUpperInvariant()}__UNLINKED_CATEGORIES");
        }

        if (UnlinkedCategories.Any(string.IsNullOrEmpty))
        {
            throw new ValidationException("empty unlinked category filter found");
        }

        if (!string.IsNullOrEmpty(UnlinkedIgnoredRootDir) && !Directory.Exists(UnlinkedIgnoredRootDir))
        {
            throw new ValidationException($"{UnlinkedIgnoredRootDir} root directory does not exist");
        }
    }
}