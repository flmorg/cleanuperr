using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cleanuparr.Persistence.Models.Configuration.ContentBlocker;

public sealed record ContentBlockerConfig : IJobConfig
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; init; } = Guid.NewGuid();
    
    public bool Enabled { get; init; }
    
    public string CronExpression { get; init; } = "0/5 * * * * ?";
    
    public bool UseAdvancedScheduling { get; init; }

    public bool IgnorePrivate { get; init; }
    
    public bool DeletePrivate { get; init; }

    public BlocklistSettings Sonarr { get; init; } = new();
    
    public BlocklistSettings Radarr { get; init; } = new();
    
    public BlocklistSettings Lidarr { get; init; } = new();
    
    public void Validate()
    {
    }
}