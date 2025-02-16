using Common.Configuration.ContentBlocker;

namespace Common.Configuration.Arr;

public abstract record ArrConfig
{
    public required bool Enabled { get; init; }

    public Block Block { get; init; } = new();
    
    public required List<ArrInstance> Instances { get; init; }
}

public readonly record struct Block
{
    public BlocklistType Type { get; init; }
    
    public string? Path { get; init; }
}