namespace Common.Configuration.ContentBlocker;

public sealed record ContentBlockerConfig : IJobConfig
{
    public bool Enabled { get; init; }
    
    public string CronExpression { get; init; } = "0 0/5 * * * ?";
    
    public bool IgnorePrivate { get; init; }
    
    public bool DeletePrivate { get; init; }

    // TODO
    public string IgnoredDownloadsPath { get; init; } = string.Empty;
    
    public BlocklistSettings Sonarr { get; init; } = new();
    
    public BlocklistSettings Radarr { get; init; } = new();
    
    public BlocklistSettings Lidarr { get; init; } = new();
    
    public void Validate()
    {
    }
}

public record BlocklistSettings
{
    public bool Enabled { get; init; }
    
    public BlocklistType Type { get; init; }
    
    public string? Path { get; init; }
}