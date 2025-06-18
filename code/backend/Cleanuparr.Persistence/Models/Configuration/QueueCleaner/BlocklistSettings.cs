using System.ComponentModel.DataAnnotations.Schema;
using Cleanuparr.Domain.Enums;

namespace Cleanuparr.Persistence.Models.Configuration.QueueCleaner;

/// <summary>
/// Settings for a blocklist
/// </summary>
[ComplexType]
public sealed record BlocklistSettings
{
    public BlocklistType BlocklistType { get; init; }
    
    public string? BlocklistPath { get; init; }
}