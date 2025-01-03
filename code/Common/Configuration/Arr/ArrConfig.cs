using Common.Configuration.ContentBlocker;

namespace Common.Configuration.Arr;

public abstract record ArrConfig
{
    public required bool Enabled { get; init; }

    public Block Block { get; init; } = new();
    
    public required List<ArrInstance> Instances { get; init; }
}

public record Block
{
    public BlocklistType Type { get; set; }
    
    public string? Path { get; set; }
}