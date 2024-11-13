namespace Common.Configuration;

public abstract record ArrConfig
{
    public required bool Enabled { get; init; }
    
    public required List<ArrInstance> Instances { get; init; }
}