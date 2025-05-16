using Common.Exceptions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Common.Configuration.DownloadCleaner;

public sealed record DownloadCleanerConfig : IJobConfig
{
    public const string SectionName = "DownloadCleaner";
    
    public bool Enabled { get; init; }

    // Trigger configuration
    [JsonProperty("cron_expression")]
    public string CronExpression { get; init; } = "0 */20 * ? * *"; // Default: every 20 minutes

    public List<CleanCategory>? Categories { get; init; }

    [JsonProperty("delete_private")]
    public bool DeletePrivate { get; init; }
    
    // TODO
    [JsonProperty("ignored_downloads_path")]
    public string? IgnoredDownloadsPath { get; init; }

    [JsonProperty("unlinked_target_category")]
    public string UnlinkedTargetCategory { get; init; } = "cleanuperr-unlinked";

    [JsonProperty("unlinked_use_tag")]
    public bool UnlinkedUseTag { get; init; }

    [JsonProperty("unlinked_ignored_root_dir")]
    public string UnlinkedIgnoredRootDir { get; init; } = string.Empty;
    
    // TODO rename to unlinked objects and add type (category, tag, etc)
    [JsonProperty("unlinked_categories")]
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