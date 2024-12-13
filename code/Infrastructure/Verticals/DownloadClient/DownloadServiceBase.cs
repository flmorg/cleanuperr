using Infrastructure.Verticals.ContentBlocker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.DownloadClient;

public abstract class DownloadServiceBase : IDownloadService
{
    protected readonly ILogger<DownloadServiceBase> _logger;
    protected readonly FilenameEvaluator _filenameEvaluator;
    protected readonly IMemoryCache _cache;
    protected readonly MemoryCacheEntryOptions _cacheOptions;
    
    protected DownloadServiceBase(
        ILogger<DownloadServiceBase> logger,
        FilenameEvaluator filenameEvaluator,
        IMemoryCache cache
    )
    {
        _logger = logger;
        _filenameEvaluator = filenameEvaluator;
        _cache = cache;
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromHours(2));
    }

    public abstract void Dispose();

    public abstract Task LoginAsync();

    public abstract Task<bool> ShouldRemoveFromArrQueueAsync(string hash, ushort maxStrikes);

    public abstract Task BlockUnwantedFilesAsync(string hash);

    protected bool StrikeAndCheckLimit(string hash, string name, ushort maxStrikes)
    {
        if (maxStrikes is 0)
        {
            return false;
        }
        
        if (!_cache.TryGetValue(hash, out int? strikeCount))
        {
            strikeCount = 1;
        }
        else
        {
            ++strikeCount;
        }
        
        _logger.LogDebug("item on strike number {strike} | {name}", strikeCount, name);
        
        if (strikeCount < maxStrikes)
        {
            _cache.Set(hash, strikeCount, _cacheOptions);
            return false;
        }

        _cache.Remove(hash);

        _logger.LogInformation("removing stalled item | {name}", name);

        return true;
    }
}