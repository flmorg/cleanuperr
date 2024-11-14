namespace Common.Configuration.ContentBlocker;

public sealed record PatternConfig
{
    public bool Enabled { get; init; }
    
    public string? Path { get; init; }
}