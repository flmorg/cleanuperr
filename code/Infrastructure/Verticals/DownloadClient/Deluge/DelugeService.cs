using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadClient;
using Common.Configuration.QueueCleaner;
using Domain.Models.Deluge.Response;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.ItemStriker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.DownloadClient.Deluge;

public sealed class DelugeService : DownloadServiceBase
{
    private readonly DelugeClient _client;
    
    public DelugeService(
        ILogger<DelugeService> logger,
        IOptions<DelugeConfig> config,
        IHttpClientFactory httpClientFactory,
        IOptions<QueueCleanerConfig> queueCleanerConfig,
        IOptions<ContentBlockerConfig> contentBlockerConfig,
        IMemoryCache cache,
        FilenameEvaluator filenameEvaluator,
        Striker striker
    ) : base(logger, queueCleanerConfig, contentBlockerConfig, cache, filenameEvaluator, striker)
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

        TorrentStatus? status = await GetTorrentStatus(hash);
        
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

        result.ShouldRemove = shouldRemove || IsItemStuckAndShouldRemove(status);
        result.IsPrivate = status.Private;
        
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

        TorrentStatus? status = await GetTorrentStatus(hash);
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

        await _client.ChangeFilesPriority(hash, sortedPriorities);

        return result;
    }
    
    /// <inheritdoc/>
    public override async Task Delete(string hash)
    {
        hash = hash.ToLowerInvariant();
        
        await _client.DeleteTorrent(hash);
    }
    
    private bool IsItemStuckAndShouldRemove(TorrentStatus status)
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

        return StrikeAndCheckLimit(status.Hash!, status.Name!);
    }

    private async Task<TorrentStatus?> GetTorrentStatus(string hash)
    {
        return await _client.SendRequest<TorrentStatus?>(
            "web.get_torrent_status",
            hash,
            new[] { "hash", "state", "name", "eta", "private", "total_done" }
        );
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