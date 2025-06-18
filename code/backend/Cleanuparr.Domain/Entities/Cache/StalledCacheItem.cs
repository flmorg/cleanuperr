namespace Cleanuparr.Domain.Entities.Cache;

public sealed record StalledCacheItem
{
    /// <summary>
    /// The amount of bytes that have been downloaded.
    /// </summary>
    public long Downloaded { get; set; }
}