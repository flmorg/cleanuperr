using Microsoft.Extensions.Configuration;

namespace Common.Configuration.Arr;

public abstract class ArrConfig
{
    public bool Enabled { get; init; }

    [ConfigurationKeyName("IMPORT_FAILED_MAX_STRIKES")]
    public short ImportFailedMaxStrikes { get; init; } = -1;
    
    public List<ArrInstance> Instances { get; init; } = [];
}

// Block struct moved to ContentBlockerConfig.cs as BlocklistSettings