using Microsoft.Extensions.Configuration;

namespace Common.Configuration.Arr;

public abstract record ArrConfig
{
    public required bool Enabled { get; init; }
    
    [ConfigurationKeyName("STALLED_MAX_STRIKES")]
    public ushort StalledMaxStrikes { get; init; }
    
    public required List<ArrInstance> Instances { get; init; }
}