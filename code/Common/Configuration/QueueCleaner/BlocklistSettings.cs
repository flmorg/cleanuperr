namespace Common.Configuration.QueueCleaner;

/// <summary>
/// Settings for a blocklist
/// </summary>
public sealed record BlocklistSettings
{
    public BlocklistType BlocklistType { get; init; }
    
    public string? BlocklistPath { get; init; }
}