using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cleanuparr.Persistence.Models.Configuration.QueueCleaner;

public sealed record QueueCleanerConfig : IJobConfig
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; init; } = Guid.NewGuid();
    
    public bool Enabled { get; init; }
    
    public string CronExpression { get; init; } = "0 0/5 * * * ?";
    
    /// <summary>
    /// Indicates whether to use the CronExpression directly or convert from a user-friendly schedule
    /// </summary>
    public bool UseAdvancedScheduling { get; init; } = false;
    
    public FailedImportConfig FailedImport { get; init; } = new();
    
    public StalledConfig Stalled { get; init; } = new();
    
    public SlowConfig Slow { get; init; } = new();
    
    public void Validate()
    {
        FailedImport.Validate();
        Stalled.Validate();
        Slow.Validate();
    }
}