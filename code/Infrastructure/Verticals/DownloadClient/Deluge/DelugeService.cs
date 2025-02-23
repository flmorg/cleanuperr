using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Attributes;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.QueueCleaner;
using Domain.Enums;
using Domain.Models.Deluge.Response;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.ItemStriker;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.DownloadClient.Deluge;

public class DelugeService : DownloadService, IDelugeService
{
    private readonly DelugeClient _client;

    public DelugeService(
        ILogger<DelugeService> logger,
        IOptions<DelugeConfig> config,
        IHttpClientFactory httpClientFactory,
        IOptions<QueueCleanerConfig> queueCleanerConfig,
        IOptions<ContentBlockerConfig> contentBlockerConfig,
        IOptions<DownloadCleanerConfig> downloadCleanerConfig,
        IMemoryCache cache,
        IFilenameEvaluator filenameEvaluator,
        IStriker striker,
        INotificationPublisher notifier,
        IDryRunInterceptor dryRunInterceptor
    ) : base(
        logger, queueCleanerConfig, contentBlockerConfig, downloadCleanerConfig, cache,
        filenameEvaluator, striker, notifier, dryRunInterceptor
    )
    {
        config.Value.Validate();
        _client = new (config, httpClientFactory);
    }
    
    public override async Task LoginAsync()
    {
        await _client.LoginAsync();
    }

    /// <inheritdoc/>
    public override async Task<StalledResult> ShouldRemoveFromArrQueueAsync(string hash)
    {
        hash = hash.ToLowerInvariant();
        
        DelugeContents? contents = null;
        StalledResult result = new();

        TorrentStatus? status = await _client.GetTorrentStatus(hash);
        
        if (status?.Hash is null)
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
            return result;
        }

        try
        {
            contents = await _client.GetTorrentFiles(hash);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "failed to find torrent {hash} in the download client", hash);
        }

        bool shouldRemove = contents?.Contents?.Count > 0;
        
        ProcessFiles(contents.Contents, (_, file) =>
        {
            if (file.Priority > 0)
            {
                shouldRemove = false;
            }
        });

        if (shouldRemove)
        {
            result.DeleteReason = DeleteReason.AllFilesBlocked;
        }
        
        result.ShouldRemove = shouldRemove || await IsItemStuckAndShouldRemove(status);
        result.IsPrivate = status.Private;

        if (!shouldRemove && result.ShouldRemove)
        {
            result.DeleteReason = DeleteReason.Stalled;
        }
        
        return result;
    }

    /// <inheritdoc/>
    public override async Task<BlockFilesResult> BlockUnwantedFilesAsync(
        string hash,
        BlocklistType blocklistType,
        ConcurrentBag<string> patterns,
        ConcurrentBag<Regex> regexes
    )
    {
        hash = hash.ToLowerInvariant();

        TorrentStatus? status = await _client.GetTorrentStatus(hash);
        BlockFilesResult result = new();
        
        if (status?.Hash is null)
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
            return result;
        }

        result.IsPrivate = status.Private;
        
        if (_contentBlockerConfig.IgnorePrivate && status.Private)
        {
            // ignore private trackers
            _logger.LogDebug("skip files check | download is private | {name}", status.Name);
            return result;
        }
        
        DelugeContents? contents = null;

        try
        {
            contents = await _client.GetTorrentFiles(hash);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "failed to find torrent {hash} in the download client", hash);
        }

        if (contents is null)
        {
            return result;
        }
        
        Dictionary<int, int> priorities = [];
        bool hasPriorityUpdates = false;
        long totalFiles = 0;
        long totalUnwantedFiles = 0;

        ProcessFiles(contents.Contents, (name, file) =>
        {
            totalFiles++;
            int priority = file.Priority;

            if (file.Priority is 0)
            {
                totalUnwantedFiles++;
            }

            if (file.Priority is not 0 && !_filenameEvaluator.IsValid(name, blocklistType, patterns, regexes))
            {
                totalUnwantedFiles++;
                priority = 0;
                hasPriorityUpdates = true;
                _logger.LogInformation("unwanted file found | {file}", file.Path);
            }
            
            priorities.Add(file.Index, priority);
        });

        if (!hasPriorityUpdates)
        {
            return result;
        }
        
        _logger.LogDebug("changing priorities | torrent {hash}", hash);

        List<int> sortedPriorities = priorities
            .OrderBy(x => x.Key)
            .Select(x => x.Value)
            .ToList();

        if (totalUnwantedFiles == totalFiles)
        {
            // Skip marking files as unwanted. The download will be removed completely.
            result.ShouldRemove = true;
            
            return result;
        }

        await _dryRunInterceptor.InterceptAsync(ChangeFilesPriority, hash, sortedPriorities);

        return result;
    }

    public override async Task<List<object>?> GetAllDownloadsToBeCleaned(List<Category> categories)
    {
        return (await _client.GetStatusForAllTorrents())
            ?.Where(x => !string.IsNullOrEmpty(x.Hash))
            .Where(x => x.State?.Equals("seeding", StringComparison.InvariantCultureIgnoreCase) is true)
            .Where(x => categories.Any(cat => cat.Name.Equals(x.Label, StringComparison.InvariantCultureIgnoreCase)))
            .Cast<object>()
            .ToList();
    }

    /// <inheritdoc/>
    public override async Task CleanDownloads(List<object> downloads, List<Category> categoriesToClean, HashSet<string> excludedHashes)
    {
        foreach (TorrentStatus download in downloads)
        {
            if (string.IsNullOrEmpty(download.Hash))
            {
                continue;
            }
            
            Category? category = categoriesToClean
                .FirstOrDefault(x => x.Name.Equals(download.Label, StringComparison.InvariantCultureIgnoreCase));

            if (category is null)
            {
                continue;
            }
            
            if (excludedHashes.Any(x => x.Equals(download.Hash, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogDebug("skip | download is used by an arr | {name}", download.Name);
                continue;
            }

            if (!_downloadCleanerConfig.DeletePrivate && download.Private)
            {
                _logger.LogDebug("skip | download is private | {name}", download.Name);
                continue;
            }
            
            ContextProvider.Set("downloadName", download.Name);
            ContextProvider.Set("hash", download.Hash);
            
            TimeSpan seedingTime = TimeSpan.FromSeconds(download.SeedingTime);
            SeedingCheckResult result = ShouldCleanDownload(download.Ratio, seedingTime, category);

            if (!result.ShouldClean)
            {
                continue;
            }

            await _dryRunInterceptor.InterceptAsync(DeleteDownload, download.Hash);

            _logger.LogInformation(
                "download cleaned | {reason} reached | {name}",
                result.Reason is CleanReason.MaxRatioReached
                    ? "MAX_RATIO & MIN_SEED_TIME"
                    : "MAX_SEED_TIME",
                download.Name
            );
            
            await _notifier.NotifyDownloadCleaned(download.Ratio, seedingTime, category.Name, result.Reason);
        }
    }
    
    /// <inheritdoc/>
    [DryRunSafeguard]
    public override async Task DeleteDownload(string hash)
    {
        hash = hash.ToLowerInvariant();
        
        await _client.DeleteTorrents([hash]);
    }
    
    [DryRunSafeguard]
    protected virtual async Task ChangeFilesPriority(string hash, List<int> sortedPriorities)
    {
        await _client.ChangeFilesPriority(hash, sortedPriorities);
    }
    
    private async Task<bool> IsItemStuckAndShouldRemove(TorrentStatus status)
    {
        if (_queueCleanerConfig.StalledMaxStrikes is 0)
        {
            return false;
        }
        
        if (_queueCleanerConfig.StalledIgnorePrivate && status.Private)
        {
            // ignore private trackers
            _logger.LogDebug("skip stalled check | download is private | {name}", status.Name);
            return false;
        }
        
        if (status.State is null || !status.State.Equals("Downloading", StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
        }

        if (status.Eta > 0)
        {
            return false;
        }
        
        ResetStrikesOnProgress(status.Hash!, status.TotalDone);

        return await StrikeAndCheckLimit(status.Hash!, status.Name!);
    }
    
    private static void ProcessFiles(Dictionary<string, DelugeFileOrDirectory>? contents, Action<string, DelugeFileOrDirectory> processFile)
    {
        if (contents is null)
        {
            return;
        }
        
        foreach (var (name, data) in contents)
        {
            switch (data.Type)
            {
                case "file":
                    processFile(name, data);
                    break;
                case "dir" when data.Contents is not null:
                    // Recurse into subdirectories
                    ProcessFiles(data.Contents, processFile);
                    break;
            }
        }
    }

    public override void Dispose()
    {
    }
}