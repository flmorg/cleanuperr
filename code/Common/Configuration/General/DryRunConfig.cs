using Microsoft.Extensions.Configuration;

namespace Common.Configuration.General;

public sealed record DryRunConfig
{
    [ConfigurationKeyName("DRY_RUN")]
    public bool IsDryRun { get; init; }
}