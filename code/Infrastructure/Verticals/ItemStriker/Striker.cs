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
    
    public bool StrikeAndCheckLimit(string key, string itemName, ushort maxStrikes)
    {
        if (maxStrikes is 0)
        {
            return false;
        }
        
        if (!_cache.TryGetValue(key, out int? strikeCount))
        {
            strikeCount = 1;
        }
        else
        {
            ++strikeCount;
        }
        
        _logger.LogDebug("item on strike number {strike} | {name}", strikeCount, itemName);
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

        _logger.LogInformation("removing item with max strikes | {name}", itemName);

        return true;
    }
}