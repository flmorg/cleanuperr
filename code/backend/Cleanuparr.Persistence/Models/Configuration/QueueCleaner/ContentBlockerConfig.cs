using System.ComponentModel.DataAnnotations.Schema;

namespace Cleanuparr.Persistence.Models.Configuration.QueueCleaner;

[ComplexType]
public sealed record ContentBlockerConfig
{
    public bool Enabled { get; init; }
    
    public bool IgnorePrivate { get; init; }
    
    public bool DeletePrivate { get; init; }

    public BlocklistSettings Sonarr { get; init; } = new();
    
    public BlocklistSettings Radarr { get; init; } = new();
    
    public BlocklistSettings Lidarr { get; init; } = new();
    
    public void Validate()
    {
    }
}
