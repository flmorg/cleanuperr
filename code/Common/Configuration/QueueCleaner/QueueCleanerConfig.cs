namespace Common.Configuration.QueueCleaner;

public sealed record QueueCleanerConfig : IJobConfig
{
    public bool Enabled { get; init; }
    
    public string CronExpression { get; init; } = "0 0/5 * * * ?";
    
    public FailedImportConfig FailedImport { get; init; } = new();
    
    public StalledConfig Stalled { get; init; } = new();
    
    public SlowConfig Slow { get; init; } = new();
    
    public ContentBlockerConfig ContentBlocker { get; init; } = new();
    
    public void Validate()
    {
        FailedImport.Validate();
        Stalled.Validate();
        Slow.Validate();
        ContentBlocker.Validate();
    }
}