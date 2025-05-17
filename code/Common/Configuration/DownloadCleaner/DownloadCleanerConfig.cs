using Common.Exceptions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Common.Configuration.DownloadCleaner;

public sealed record DownloadCleanerConfig : IJobConfig
{
    public const string SectionName = "DownloadCleaner";
    
    public bool Enabled { get; init; }

    public string CronExpression { get; init; } = "0 0 * * * ?";

    public List<CleanCategory> Categories { get; init; } = [];

    public bool DeletePrivate { get; init; }
    
    public string IgnoredDownloadsPath { get; init; } = string.Empty;

    public string UnlinkedTargetCategory { get; init; } = "cleanuperr-unlinked";

    public bool UnlinkedUseTag { get; init; }

    public string UnlinkedIgnoredRootDir { get; init; } = string.Empty;
    
    // TODO rename to unlinked objects and add type (category, tag, etc)
    public List<string> UnlinkedCategories { get; init; } = [];

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