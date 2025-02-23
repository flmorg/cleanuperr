using Common.Helpers;
using Domain.Enums;
using Infrastructure.Helpers;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.ItemStriker;

public sealed class Striker : IStriker
{
    private readonly ILogger<Striker> _logger;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;
    private readonly INotificationPublisher _notifier;
    private readonly IDryRunInterceptor _dryRunInterceptor;

    public Striker(ILogger<Striker> logger, IMemoryCache cache, INotificationPublisher notifier, IDryRunInterceptor dryRunInterceptor)
    {
        _logger = logger;
        _cache = cache;
        _notifier = notifier;
        _dryRunInterceptor = dryRunInterceptor;
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(StaticConfiguration.TriggerValue + Constants.CacheLimitBuffer);
    }
    
    public async Task<bool> StrikeAndCheckLimit(string hash, string itemName, ushort maxStrikes, StrikeType strikeType)
    {
        if (maxStrikes is 0)
        {
            return false;
        }
        
        string key = CacheKeys.Strike(strikeType, hash);
        
        if (!_cache.TryGetValue(key, out int strikeCount))
        {
            strikeCount = 1;
        }
        else
        {
            ++strikeCount;
        }
        
        _logger.LogInformation("item on strike number {strike} | reason {reason} | {name}", strikeCount, strikeType.ToString(), itemName);

        await _notifier.NotifyStrike(strikeType, strikeCount);
        
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