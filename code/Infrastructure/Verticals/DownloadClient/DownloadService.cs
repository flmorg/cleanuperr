using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.QueueCleaner;
using Common.CustomDataTypes;
using Common.Helpers;
using Domain.Enums;
using Domain.Models.Cache;
using Infrastructure.Configuration;
using Infrastructure.Helpers;
using Infrastructure.Http;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.Files;
using Infrastructure.Verticals.ItemStriker;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.DownloadClient;

public abstract class DownloadService : IDownloadService
{
    protected readonly ILogger<DownloadService> _logger;
    protected readonly IConfigManager _configManager;
    protected readonly IMemoryCache _cache;
    protected readonly IFilenameEvaluator _filenameEvaluator;
    protected readonly IStriker _striker;
    protected readonly MemoryCacheEntryOptions _cacheOptions;
    protected readonly INotificationPublisher _notifier;
    protected readonly IDryRunInterceptor _dryRunInterceptor;
    protected readonly IHardLinkFileService _hardLinkFileService;
    protected readonly IDynamicHttpClientProvider _httpClientProvider;
    
    // Client-specific configuration
    protected ClientConfig _clientConfig;
    
    // HTTP client for this service
    protected HttpClient? _httpClient;

    protected DownloadService(
        ILogger<DownloadService> logger,
        IConfigManager configManager,
        IMemoryCache cache,
        IFilenameEvaluator filenameEvaluator,
        IStriker striker,
        INotificationPublisher notifier,
        IDryRunInterceptor dryRunInterceptor,
        IHardLinkFileService hardLinkFileService,
        IDynamicHttpClientProvider httpClientProvider
    )
    {
        _logger = logger;
        _configManager = configManager;
        _cache = cache;
        _filenameEvaluator = filenameEvaluator;
        _striker = striker;
        _notifier = notifier;
        _dryRunInterceptor = dryRunInterceptor;
        _hardLinkFileService = hardLinkFileService;
        _httpClientProvider = httpClientProvider;
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(StaticConfiguration.TriggerValue + Constants.CacheLimitBuffer);
        
        // Initialize with default empty configuration
        _clientConfig = new ClientConfig();
    }
    
    /// <inheritdoc />
    public Guid GetClientId()
    {
        return _clientConfig.Id;
    }
    
    /// <inheritdoc />
    public virtual void Initialize(ClientConfig clientConfig)
    {
        _clientConfig = clientConfig;
        
        // Create HTTP client for this service
        _httpClient = _httpClientProvider.CreateClient(clientConfig);
        
        _logger.LogDebug("Initialized download service for client {clientId} ({type})", 
            clientConfig.Id, clientConfig.Type);
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
    
    protected void ResetStalledStrikesOnProgress(string hash, long downloaded)
    {
        var queueCleanerConfig = _configManager.GetQueueCleanerConfig();
        if (queueCleanerConfig == null || !queueCleanerConfig.StalledResetStrikesOnProgress)
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
        var queueCleanerConfig = _configManager.GetQueueCleanerConfig();
        if (queueCleanerConfig == null || !queueCleanerConfig.SlowResetStrikesOnProgress)
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
        var queueCleanerConfig = _configManager.GetQueueCleanerConfig();
        if (queueCleanerConfig == null || !queueCleanerConfig.SlowResetStrikesOnProgress)
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
            
            var queueCleanerConfig = _configManager.GetQueueCleanerConfig();
            ushort maxStrikes = queueCleanerConfig?.SlowMaxStrikes ?? 0;
            bool shouldRemove = await _striker
                .StrikeAndCheckLimit(downloadHash, downloadName, maxStrikes, StrikeType.SlowSpeed);

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
            
            var queueCleanerConfig = _configManager.GetQueueCleanerConfig();
            ushort maxStrikes = queueCleanerConfig?.SlowMaxStrikes ?? 0;
            bool shouldRemove = await _striker
                .StrikeAndCheckLimit(downloadHash, downloadName, maxStrikes, StrikeType.SlowTime);

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