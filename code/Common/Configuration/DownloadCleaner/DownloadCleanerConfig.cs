using Common.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Common.Configuration.DownloadCleaner;

public sealed record DownloadCleanerConfig : IJobConfig
{
    public const string SectionName = "DownloadCleaner";
    
    public bool Enabled { get; init; }

    public List<CleanCategory>? Categories { get; init; }

    [ConfigurationKeyName("DELETE_PRIVATE")]
    public bool DeletePrivate { get; init; }

    [ConfigurationKeyName("NO_HARDLINKS_CATEGORY")]
    public string NoHardlinksCategory { get; init; } = "";

    [ConfigurationKeyName("HARDLINK_CATEGORIES")]
    public List<string>? HardlinkCategories { get; init; }

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
        
        if (string.IsNullOrEmpty(NoHardlinksCategory))
        {
            return;
        }

        if (HardlinkCategories?.Count is null or 0)
        {
            throw new ValidationException("no categories configured");
        }

        if (HardlinkCategories.Contains(NoHardlinksCategory))
        {
            throw new ValidationException("NO_HARDLINKS_CATEGORY is present in the list of filtered categories");
        }

        if (HardlinkCategories.Any(string.IsNullOrEmpty))
        {
            throw new ValidationException("empty hardlink filter category found");
        }
    }
}