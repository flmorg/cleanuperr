namespace Common.Configuration;

public sealed class QuartzConfig
{
    public required string QueueCleanerTrigger { get; init; }
    
    [Obsolete]
    public string? BlockedTorrentTrigger { get; init; }
}