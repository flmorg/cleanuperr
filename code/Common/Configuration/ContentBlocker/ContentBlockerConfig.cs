namespace Common.Configuration.ContentBlocker;

public sealed record ContentBlockerConfig : IJobConfig
{
    public const string SectionName = "ContentBlocker";
    
    public required bool Enabled { get; init; }
    
    public void Validate()
    {
    }
}