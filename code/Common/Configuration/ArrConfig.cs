namespace Common.Configuration;

public abstract record ArrConfig
{
    public required List<ArrInstance> Instances { get; init; }
}