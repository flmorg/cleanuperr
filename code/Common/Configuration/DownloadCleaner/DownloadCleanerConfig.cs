using Microsoft.Extensions.Configuration;

namespace Common.Configuration.DownloadCleaner;

public sealed record DownloadCleanerConfig : IJobConfig
{
    public const string SectionName = "DownloadCleaner";
    
    public bool Enabled { get; init; }
    
    public List<Category>? Categories { get; init; }

    [ConfigurationKeyName("REMOVE_PRIVATE")]
    public bool RemovePrivate { get; set; }

    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }
        
        Categories?.ForEach(x => x.Validate());
    }
}