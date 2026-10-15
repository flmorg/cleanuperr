using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cleanuparr.Domain.Exceptions;

namespace Cleanuparr.Persistence.Models.Configuration.DownloadCleaner;

public sealed record UnlinkedConfig : IConfig
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DownloadClientConfigId { get; set; }

    public DownloadClientConfig DownloadClientConfig { get; set; } = null!;

    public bool Enabled { get; set; } = false;

    public string TargetCategory { get; set; } = "cleanuparr-unlinked";

    public bool UseTag { get; set; }

    public List<string> IgnoredRootDirs { get; set; } = [];

    public List<string> Categories { get; set; } = [];

    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(TargetCategory))
        {
            throw new CustomValidationException("Unlinked target category is required");
        }

        if (Categories.Count is 0)
        {
            throw new CustomValidationException("No unlinked categories configured");
        }

        if (Categories.Contains(TargetCategory, StringComparer.OrdinalIgnoreCase))
        {
            throw new CustomValidationException("The unlinked target category should not be present in unlinked categories");
        }

        if (Categories.Any(string.IsNullOrWhiteSpace))
        {
            throw new CustomValidationException("Empty unlinked category filter found");
        }

        foreach (var dir in IgnoredRootDirs.Where(d => !string.IsNullOrEmpty(d)))
        {
            if (!Directory.Exists(dir))
            {
                throw new CustomValidationException($"{dir} root directory does not exist or is not accessible (check permissions)");
            }
        }
    }
}
