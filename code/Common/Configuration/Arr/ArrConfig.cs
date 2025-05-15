using Common.Configuration.ContentBlocker;
using Microsoft.Extensions.Configuration;

namespace Common.Configuration.Arr;

public abstract class ArrConfig
{
    public bool Enabled { get; init; }

    public Block Block { get; init; } = new();

    [ConfigurationKeyName("IMPORT_FAILED_MAX_STRIKES")]
    public short ImportFailedMaxStrikes { get; init; } = -1;
    
    public List<ArrInstance> Instances { get; init; }
}

public readonly record struct Block
{
    public BlocklistType Type { get; init; }
    
    public string? Path { get; init; }
}