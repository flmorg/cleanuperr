using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.QueueCleaner;
using Common.CustomDataTypes;
using Common.Helpers;
using Domain.Enums;
using Domain.Models.Cache;
using Infrastructure.Helpers;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.ItemStriker;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.DownloadClient;

public abstract class DownloadService : IDownloadService
{
    protected readonly ILogger<DownloadService> _logger;
    protected readonly QueueCleanerConfig _queueCleanerConfig;
    protected readonly ContentBlockerConfig _contentBlockerConfig;
    protected readonly DownloadCleanerConfig _downloadCleanerConfig;
    protected readonly IMemoryCache _cache;
    protected readonly IFilenameEvaluator _filenameEvaluator;
    protected readonly IStriker _striker;
    protected readonly MemoryCacheEntryOptions _cacheOptions;
    protected readonly INotificationPublisher _notifier;
    protected readonly IDryRunInterceptor _dryRunInterceptor;

    protected DownloadService(
        ILogger<DownloadService> logger,
        IOptions<QueueCleanerConfig> queueCleanerConfig,
        IOptions<ContentBlockerConfig> contentBlockerConfig,
        IOptions<DownloadCleanerConfig> downloadCleanerConfig,
        IMemoryCache cache,
        IFilenameEvaluator filenameEvaluator,
        IStriker striker,
        INotificationPublisher notifier,
        IDryRunInterceptor dryRunInterceptor
    )
    {
        _logger = logger;
        _queueCleanerConfig = queueCleanerConfig.Value;
        _contentBlockerConfig = contentBlockerConfig.Value;
        _downloadCleanerConfig = downloadCleanerConfig.Value;
        _cache = cache;
        _filenameEvaluator = filenameEvaluator;
        _striker = striker;
        _notifier = notifier;
        _dryRunInterceptor = dryRunInterceptor;
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(StaticConfiguration.TriggerValue + Constants.CacheLimitBuffer);
    }

    public abstract void Dispose();

    public abstract Task LoginAsync();

    public abstract Task<DownloadCheckResult> ShouldRemoveFromArrQueueAsync(string hash, IReadOnlyList<string> ignoredDownloads);

    /// <inheritdoc/>
    public abstract Task<BlockFilesResult> BlockUnwantedFilesAsync(string hash,
        BlocklistType blocklistType,
        ConcurrentBag<string> patterns,
        ConcurrentBag<Regex> regexes, IReadOnlyList<string> ignoredDownloads);

    /// <inheritdoc/>
    public abstract Task DeleteDownload(string hash);

    /// <inheritdoc/>
    public abstract Task<List<object>?> GetAllDownloadsToBeCleaned(List<Category> categories);

    /// <inheritdoc/>
    public abstract Task CleanDownloads(List<object> downloads, List<Category> categoriesToClean, HashSet<string> excludedHashes,
        IReadOnlyList<string> ignoredDownloads);

    protected void ResetStalledStrikesOnProgress(string hash, long downloaded)
    {
        if (!_queueCleanerConfig.StalledResetStrikesOnProgress)
        {
            return;
        }

        if (_cache.TryGetValue(CacheKeys.StrikeItem(hash, StrikeType.Stalled), out StalledCacheItem? cachedItem) &&
            cachedItem is not null && downloaded > cachedItem.Downloaded)
        {
            // cache item found
            _cache.Remove(CacheKeys.Strike(StrikeType.Stalled, hash));
            _logger.LogDebug("resetting stalled strikes for {hash} due to progress", hash);
        }
        
        _cache.Set(CacheKeys.StrikeItem(hash, StrikeType.Stalled), new StalledCacheItem { Downloaded = downloaded }, _cacheOptions);
    }
    
    protected void ResetSlowSpeedStrikesOnProgress(string downloadName, string hash)
    {
        if (!_queueCleanerConfig.SlowResetStrikesOnProgress)
        {
            return;
        }

        string key = CacheKeys.Strike(StrikeType.SlowSpeed, hash);

        if (!_cache.TryGetValue(key, out object? value) || value is null)
        {
            return;
        }
        
        _cache.Remove(key);
        _logger.LogDebug("resetting slow speed strikes due to progress | {name}", downloadName);
    }
    
    protected void ResetSlowTimeStrikesOnProgress(string downloadName, string hash)
    {
        if (!_queueCleanerConfig.SlowResetStrikesOnProgress)
        {
            return;
        }

        string key = CacheKeys.Strike(StrikeType.SlowTime, hash);

        if (!_cache.TryGetValue(key, out object? value) || value is null)
        {
            return;
        }
        
        _cache.Remove(key);
        _logger.LogDebug("resetting slow time strikes due to progress | {name}", downloadName);
    }

    protected async Task<(bool ShouldRemove, DeleteReason Reason)> CheckIfSlow(
        string downloadHash,
        string downloadName,
        ByteSize minSpeed,
        ByteSize currentSpeed,
        SmartTimeSpan maxTime,
        SmartTimeSpan currentTime
    )
    {
        if (minSpeed.Bytes > 0 && currentSpeed < minSpeed)
        {
            _logger.LogTrace("slow speed | {speed}/s | {name}", currentSpeed.ToString(), downloadName);
            
            bool shouldRemove = await _striker
                .StrikeAndCheckLimit(downloadHash, downloadName, _queueCleanerConfig.SlowMaxStrikes, StrikeType.SlowSpeed);

            if (shouldRemove)
            {
                return (true, DeleteReason.SlowSpeed);
            }
        }
        else
        {
            ResetSlowSpeedStrikesOnProgress(downloadName, downloadHash);
        }
        
        if (maxTime.Time > TimeSpan.Zero && currentTime > maxTime)
        {
            _logger.LogTrace("slow estimated time | {time} | {name}", currentTime.ToString(), downloadName);
            
            bool shouldRemove = await _striker
                .StrikeAndCheckLimit(downloadHash, downloadName, _queueCleanerConfig.SlowMaxStrikes, StrikeType.SlowTime);

            if (shouldRemove)
            {
                return (true, DeleteReason.SlowTime);
            }
        }
        else
        {
            ResetSlowTimeStrikesOnProgress(downloadName, downloadHash);
        }
        
        return (false, DeleteReason.None);
    }
    
    protected SeedingCheckResult ShouldCleanDownload(double ratio, TimeSpan seedingTime, Category category)
    {
        // check ratio
        if (DownloadReachedRatio(ratio, seedingTime, category))
        {
            return new()
            {
                ShouldClean = true,
                Reason = CleanReason.MaxRatioReached
            };
        }
            
        // check max seed time
        if (DownloadReachedMaxSeedTime(seedingTime, category))
        {
            return new()
            {
                ShouldClean = true,
                Reason = CleanReason.MaxSeedTimeReached
            };
        }

        return new();
    }

    private bool DownloadReachedRatio(double ratio, TimeSpan seedingTime, Category category)
    {
        if (category.MaxRatio < 0)
        {
            return false;
        }
        
        string downloadName = ContextProvider.Get<string>("downloadName");
        TimeSpan minSeedingTime = TimeSpan.FromHours(category.MinSeedTime);
        
        if (category.MinSeedTime > 0 && seedingTime < minSeedingTime)
        {
            _logger.LogDebug("skip | download has not reached MIN_SEED_TIME | {name}", downloadName);
            return false;
        }

        if (ratio < category.MaxRatio)
        {
            _logger.LogDebug("skip | download has not reached MAX_RATIO | {name}", downloadName);
            return false;
        }
        
        // max ration is 0 or reached
        return true;
    }
    
    private bool DownloadReachedMaxSeedTime(TimeSpan seedingTime, Category category)
    {
        if (category.MaxSeedTime < 0)
        {
            return false;
        }
        
        string downloadName = ContextProvider.Get<string>("downloadName");
        TimeSpan maxSeedingTime = TimeSpan.FromHours(category.MaxSeedTime);
        
        if (category.MaxSeedTime > 0 && seedingTime < maxSeedingTime)
        {
            _logger.LogDebug("skip | download has not reached MAX_SEED_TIME | {name}", downloadName);
            return false;
        }

        // max seed time is 0 or reached
        return true;
    }
}