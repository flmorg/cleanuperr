using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.QueueCleaner;
using Common.Helpers;
using Domain.Enums;
using Domain.Models.Cache;
using Infrastructure.Helpers;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.Files;
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
    protected readonly IHardLinkFileService _hardLinkFileService;

    protected DownloadService(
        ILogger<DownloadService> logger,
        IOptions<QueueCleanerConfig> queueCleanerConfig,
        IOptions<ContentBlockerConfig> contentBlockerConfig,
        IOptions<DownloadCleanerConfig> downloadCleanerConfig,
        IMemoryCache cache,
        IFilenameEvaluator filenameEvaluator,
        IStriker striker,
        INotificationPublisher notifier,
        IDryRunInterceptor dryRunInterceptor,
        IHardLinkFileService hardLinkFileService
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
        _hardLinkFileService = hardLinkFileService;
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(StaticConfiguration.TriggerValue + Constants.CacheLimitBuffer);
    }

    public abstract void Dispose();

    public abstract Task LoginAsync();

    public abstract Task<StalledResult> ShouldRemoveFromArrQueueAsync(string hash, IReadOnlyList<string> ignoredDownloads);

    /// <inheritdoc/>
    public abstract Task<BlockFilesResult> BlockUnwantedFilesAsync(string hash,
        BlocklistType blocklistType,
        ConcurrentBag<string> patterns,
        ConcurrentBag<Regex> regexes, IReadOnlyList<string> ignoredDownloads);

    /// <inheritdoc/>
    public abstract Task DeleteDownload(string hash);

    /// <inheritdoc/>
    public abstract Task<List<object>?> GetSeedingDownloads();
    
    /// <inheritdoc/>
    public abstract List<object>? FilterDownloadsToBeCleanedAsync(List<object>? downloads, List<CleanCategory> categories);

    /// <inheritdoc/>
    public abstract List<object>? FilterDownloadsToChangeCategoryAsync(List<object>? downloads, List<string> categories);

    /// <inheritdoc/>
    public abstract Task CleanDownloadsAsync(List<object>? downloads, List<CleanCategory> categoriesToClean, HashSet<string> excludedHashes, IReadOnlyList<string> ignoredDownloads);

    /// <inheritdoc/>
    public abstract Task ChangeCategoryForNoHardLinksAsync(List<object>? downloads, HashSet<string> excludedHashes, IReadOnlyList<string> ignoredDownloads);
    
    /// <inheritdoc/>
    public abstract Task CreateCategoryAsync(string name);
    
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
    /// <param name="strikeType"></param>
    /// <returns>True if the limit has been reached; otherwise, false.</returns>
    protected async Task<bool> StrikeAndCheckLimit(string hash, string itemName, StrikeType strikeType)
    {
        return await _striker.StrikeAndCheckLimit(hash, itemName, _queueCleanerConfig.StalledMaxStrikes, strikeType);
    }
    
    protected SeedingCheckResult ShouldCleanDownload(double ratio, TimeSpan seedingTime, CleanCategory category)
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
    
    protected string? GetRootWithFirstDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string? root = Path.GetPathRoot(path);
        
        if (root is null)
        {
            return null;
        }

        string relativePath = path[root.Length..].TrimStart(Path.DirectorySeparatorChar);
        string[] parts = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        return parts.Length > 0 ? Path.Combine(root, parts[0]) : root;
    }
    
    private bool DownloadReachedRatio(double ratio, TimeSpan seedingTime, CleanCategory category)
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
    
    private bool DownloadReachedMaxSeedTime(TimeSpan seedingTime, CleanCategory category)
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