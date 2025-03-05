using Domain.Enums;

namespace Infrastructure.Helpers;

public static class CacheKeys
{
    public static string Strike(StrikeType strikeType, string hash) => $"{strikeType.ToString()}_{hash}";

    public static string BlocklistType(InstanceType instanceType) => $"{instanceType.ToString()}_type";
    public static string BlocklistPatterns(InstanceType instanceType) => $"{instanceType.ToString()}_patterns";
    public static string BlocklistRegexes(InstanceType instanceType) => $"{instanceType.ToString()}_regexes";
    
    public static string Item(string hash) => $"item_{hash}";
    
    public static string IgnoredDownloads(string name) => $"{name}_ignored";
}