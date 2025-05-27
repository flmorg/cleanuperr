using Microsoft.Extensions.Configuration;

namespace Common.Configuration.Arr;

public abstract class ArrConfig : IConfig
{
    public bool Enabled { get; init; }

    public short FailedImportMaxStrikes { get; init; } = -1;
    
    public List<ArrInstance> Instances { get; init; } = [];

    public abstract void Validate();
}