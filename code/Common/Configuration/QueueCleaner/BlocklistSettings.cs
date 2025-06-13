using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Configuration.QueueCleaner;

/// <summary>
/// Settings for a blocklist
/// </summary>
[ComplexType]
public sealed record BlocklistSettings
{
    public BlocklistType BlocklistType { get; init; }
    
    public string? BlocklistPath { get; init; }
}