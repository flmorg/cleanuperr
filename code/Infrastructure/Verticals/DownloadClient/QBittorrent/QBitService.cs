using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadClient;
using Common.Configuration.QueueCleaner;
using Common.Helpers;
using Domain.Enums;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.ItemStriker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QBittorrent.Client;

namespace Infrastructure.Verticals.DownloadClient.QBittorrent;

public sealed class QBitService : DownloadServiceBase
{
    private readonly QBitConfig _config;
    private readonly QBittorrentClient _client;

    public QBitService(
        ILogger<QBitService> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<QBitConfig> config,
        IOptions<QueueCleanerConfig> queueCleanerConfig,
        IOptions<ContentBlockerConfig> contentBlockerConfig,
        IMemoryCache cache,
        FilenameEvaluator filenameEvaluator,
        Striker striker
    ) : base(logger, queueCleanerConfig, contentBlockerConfig, cache, filenameEvaluator, striker)
    {
        _config = config.Value;
        _config.Validate();
        _client = new(httpClientFactory.CreateClient(Constants.HttpClientWithRetryName), _config.Url);
    }

    public override async Task LoginAsync()
    {
        if (string.IsNullOrEmpty(_config.Username) && string.IsNullOrEmpty(_config.Password))
        {
            return;
        }
        
        await _client.LoginAsync(_config.Username, _config.Password);
    }

    /// <inheritdoc/>
    public override async Task<StalledResult> ShouldRemoveFromArrQueueAsync(string hash)
    {
        StalledResult result = new();
        TorrentInfo? torrent = (await _client.GetTorrentListAsync(new TorrentListQuery { Hashes = [hash] }))
            .FirstOrDefault();

        if (torrent is null)
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
            return result;
        }

        TorrentProperties? torrentProperties = await _client.GetTorrentPropertiesAsync(hash);

        if (torrentProperties is null)
        {
            _logger.LogDebug("failed to find torrent properties {hash} in the download client", hash);
            return result;
        }

        result.IsPrivate = torrentProperties.AdditionalData.TryGetValue("is_private", out var dictValue) &&
                           bool.TryParse(dictValue?.ToString(), out bool boolValue)
                           && boolValue;

        // if all files were blocked by qBittorrent
        if (torrent is { CompletionOn: not null, Downloaded: null or 0 })
        {
            result.ShouldRemove = true;
            result.DeleteReason = DeleteReason.AllFilesBlocked;
            return result;
        }

        IReadOnlyList<TorrentContent>? files = await _client.GetTorrentContentsAsync(hash);

        // if all files are marked as skip
        if (files?.Count is > 0 && files.All(x => x.Priority is TorrentContentPriority.Skip))
        {
            result.ShouldRemove = true;
            result.DeleteReason = DeleteReason.AllFilesBlocked;
            return result;
        }

        result.ShouldRemove = await IsItemStuckAndShouldRemove(torrent, result.IsPrivate);

        if (result.ShouldRemove)
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
        TorrentInfo? torrent = (await _client.GetTorrentListAsync(new TorrentListQuery { Hashes = [hash] }))
            .FirstOrDefault();
        BlockFilesResult result = new();

        if (torrent is null)
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
            return result;
        }
        
        TorrentProperties? torrentProperties = await _client.GetTorrentPropertiesAsync(hash);

        if (torrentProperties is null)
        {
            _logger.LogDebug("failed to find torrent properties {hash} in the download client", hash);
            return result;
        }

        bool isPrivate = torrentProperties.AdditionalData.TryGetValue("is_private", out var dictValue) &&
                         bool.TryParse(dictValue?.ToString(), out bool boolValue)
                         && boolValue;

        result.IsPrivate = isPrivate;

        if (_contentBlockerConfig.IgnorePrivate && isPrivate)
        {
            // ignore private trackers
            _logger.LogDebug("skip files check | download is private | {name}", torrent.Name);
            return result;
        }
        
        IReadOnlyList<TorrentContent>? files = await _client.GetTorrentContentsAsync(hash);

        if (files is null)
        {
            return result;
        }

        List<int> unwantedFiles = [];
        long totalFiles = 0;
        long totalUnwantedFiles = 0;
        
        foreach (TorrentContent file in files)
        {
            if (!file.Index.HasValue)
            {
                continue;
            }

            totalFiles++;

            if (file.Priority is TorrentContentPriority.Skip)
            {
                totalUnwantedFiles++;
                continue;
            }

            if (_filenameEvaluator.IsValid(file.Name, blocklistType, patterns, regexes))
            {
                continue;
            }
            
            _logger.LogInformation("unwanted file found | {file}", file.Name);
            unwantedFiles.Add(file.Index.Value);
            totalUnwantedFiles++;
        }

        if (unwantedFiles.Count is 0)
        {
            return result;
        }
        
        if (totalUnwantedFiles == totalFiles)
        {
            // Skip marking files as unwanted. The download will be removed completely.
            result.ShouldRemove = true;
            
            return result;
        }

        foreach (int fileIndex in unwantedFiles)
        {
            await _client.SetFilePriorityAsync(hash, fileIndex, TorrentContentPriority.Skip);
        }
        
        return result;
    }

    /// <inheritdoc/>
    public override async Task Delete(string hash)
    {
        await _client.DeleteAsync(hash, deleteDownloadedData: true);
    }

    public override void Dispose()
    {
        _client.Dispose();
    }
    
    private async Task<bool> IsItemStuckAndShouldRemove(TorrentInfo torrent, bool isPrivate)
    {
        if (_queueCleanerConfig.StalledMaxStrikes is 0)
        {
            return false;
        }
        
        if (_queueCleanerConfig.StalledIgnorePrivate && isPrivate)
        {
            // ignore private trackers
            _logger.LogDebug("skip stalled check | download is private | {name}", torrent.Name);
            return false;
        }
        
        if (torrent.State is not TorrentState.StalledDownload and not TorrentState.FetchingMetadata
            and not TorrentState.ForcedFetchingMetadata)
        {
            // ignore other states
            return false;
        }

        ResetStrikesOnProgress(torrent.Hash, torrent.Downloaded ?? 0);

        return await StrikeAndCheckLimit(torrent.Hash, torrent.Name);
    }
}