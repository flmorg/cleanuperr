using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.ContentBlocker;
using Common.Configuration.QueueCleaner;
using Common.Helpers;
using Domain.Enums;
using Domain.Models.Cache;
using Infrastructure.Helpers;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.ItemStriker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.DownloadClient;

public abstract class DownloadServiceBase : IDownloadService
{
    protected readonly ILogger<DownloadServiceBase> _logger;
    protected readonly QueueCleanerConfig _queueCleanerConfig;
    protected readonly ContentBlockerConfig _contentBlockerConfig;
    protected readonly IMemoryCache _cache;
    protected readonly FilenameEvaluator _filenameEvaluator;
    protected readonly Striker _striker;
    protected readonly MemoryCacheEntryOptions _cacheOptions;
    
    protected DownloadServiceBase(
        ILogger<DownloadServiceBase> logger,
        IOptions<QueueCleanerConfig> queueCleanerConfig,
        IOptions<ContentBlockerConfig> contentBlockerConfig,
        IMemoryCache cache,
        FilenameEvaluator filenameEvaluator,
        Striker striker
    )
    {
        _logger = logger;
        _queueCleanerConfig = queueCleanerConfig.Value;
        _contentBlockerConfig = contentBlockerConfig.Value;
        _cache = cache;
        _filenameEvaluator = filenameEvaluator;
        _striker = striker;
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(StaticConfiguration.TriggerValue + Constants.CacheLimitBuffer);
    }

    public abstract void Dispose();

    public abstract Task LoginAsync();

    public abstract Task<StalledResult> ShouldRemoveFromArrQueueAsync(string hash);

    /// <inheritdoc/>
    public abstract Task<BlockFilesResult> BlockUnwantedFilesAsync(
        string hash,
        BlocklistType blocklistType,
        ConcurrentBag<string> patterns,
        ConcurrentBag<Regex> regexes
    );

    /// <inheritdoc/>
    public abstract Task Delete(string hash);

    protected void ResetStrikesOnProgress(string hash, long downloaded)
    {
        if (!_queueCleanerConfig.StalledResetStrikesOnProgress)
        {
            return;
        }
        
        if (_cache.TryGetValue(CacheKeys.Item(hash), out CacheItem? cachedItem) && cachedItem is not null && downloaded > cachedItem.Downloaded)
        {
            // cache item found
            _cache.Remove(CacheKeys.Strike(StrikeType.Stalled, hash));
            _logger.LogDebug("resetting strikes for {hash} due to progress", hash);
        }
        
        _cache.Set(CacheKeys.Item(hash), new CacheItem { Downloaded = downloaded }, _cacheOptions);
    }

    /// <summary>
    /// Strikes an item and checks if the limit has been reached.
    /// </summary>
    /// <param name="hash">The torrent hash.</param>
    /// <param name="itemName">The name or title of the item.</param>
    /// <returns>True if the limit has been reached; otherwise, false.</returns>
    protected async Task<bool> StrikeAndCheckLimit(string hash, string itemName)
    {
        return await _striker.StrikeAndCheckLimit(hash, itemName, _queueCleanerConfig.StalledMaxStrikes, StrikeType.Stalled);
    }
}