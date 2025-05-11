using Microsoft.Extensions.Configuration;

namespace Common.Configuration.General;

public sealed record SearchConfig
{
    [ConfigurationKeyName("SEARCH_ENABLED")]
    public bool SearchEnabled { get; init; } = true;
    
    [ConfigurationKeyName("SEARCH_DELAY")]
    public ushort SearchDelay { get; init; } = 30;
}