using Microsoft.Extensions.Configuration;

namespace Common.Configuration.ContentBlocker;

public sealed record ContentBlockerConfig : IJobConfig
{
    public const string SectionName = "ContentBlocker";
    
    public required bool Enabled { get; init; }
    
    [ConfigurationKeyName("IGNORE_PRIVATE")]
    public bool IgnorePrivate { get; init; }
    
    public PatternConfig? Blacklist { get; init; }
    
    public PatternConfig? Whitelist { get; init; }

    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (Blacklist is null && Whitelist is null)
        {
            throw new Exception("content blocker is enabled, but both blacklist and whitelist are missing");
        }

        if (Blacklist?.Enabled is true && Whitelist?.Enabled is true)
        {
            throw new Exception("only one exclusion (blacklist/whitelist) list is allowed");
        }

        if (Blacklist?.Enabled is true && string.IsNullOrEmpty(Blacklist.Path))
        {
            throw new Exception("blacklist path is required");
        }
        
        if (Whitelist?.Enabled is true && string.IsNullOrEmpty(Whitelist.Path))
        {
            throw new Exception("blacklist path is required");
        }
    }
}