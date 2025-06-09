using Common.Exceptions;

namespace Common.Configuration.DownloadCleaner;

public sealed record DownloadCleanerConfig : IJobConfig
{
    public bool Enabled { get; init; }

    public string CronExpression { get; init; } = "0 0 * * * ?";

    /// <summary>
    /// Indicates whether to use the CronExpression directly or convert from a user-friendly schedule
    /// </summary>
    public bool UseAdvancedScheduling { get; init; } = false;

    public List<CleanCategory> Categories { get; init; } = [];

    public bool DeletePrivate { get; init; }
    
    public string UnlinkedTargetCategory { get; init; } = "cleanuparr-unlinked";

    public bool UnlinkedUseTag { get; init; }

    public string UnlinkedIgnoredRootDir { get; init; } = string.Empty;
    
    public List<string> UnlinkedCategories { get; init; } = [];

    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }
        
        if (Categories.GroupBy(x => x.Name).Any(x => x.Count() > 1))
        {
            throw new ValidationException("duplicated clean categories found");
        }
        
        Categories.ForEach(x => x.Validate());
        
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
            throw new ValidationException($"The unlinked target category should not be present in unlinked categories");
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