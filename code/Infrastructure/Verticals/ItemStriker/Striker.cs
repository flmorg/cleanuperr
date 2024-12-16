using Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.ItemStriker;

public class Striker
{
    private readonly ILogger<Striker> _logger;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public Striker(ILogger<Striker> logger, IMemoryCache cache)
    {
        _logger = logger;
        _cache = cache;
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromHours(2));
    }
    
    public bool StrikeAndCheckLimit(string hash, string itemName, ushort maxStrikes, StrikeType strikeType)
    {
        if (maxStrikes is 0)
        {
            return false;
        }
        
        string key = $"{strikeType.ToString()}_{hash}";
        
        if (!_cache.TryGetValue(key, out int? strikeCount))
        {
            strikeCount = 1;
        }
        else
        {
            ++strikeCount;
        }
        
        _logger.LogDebug("item on strike number {strike} | reason {reason} | {name}", strikeCount, strikeType.ToString(), itemName);
        _cache.Set(key, strikeCount, _cacheOptions);
        
        if (strikeCount < maxStrikes)
        {
            return false;
        }

        if (strikeCount > maxStrikes)
        {
            _logger.LogWarning("blocked item keeps coming back | {name}", itemName);
            _logger.LogWarning("be sure to enable \"Reject Blocklisted Torrent Hashes While Grabbing\" on your indexers to reject blocked items");
        }

        _logger.LogInformation("removing item with max strikes | reason {reason} | {name}", strikeType.ToString(), itemName);

        return true;
    }
}