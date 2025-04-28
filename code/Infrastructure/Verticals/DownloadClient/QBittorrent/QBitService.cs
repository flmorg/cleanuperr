using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Attributes;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.QueueCleaner;
using Common.CustomDataTypes;
using Common.Helpers;
using Domain.Enums;
using Infrastructure.Extensions;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.ItemStriker;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QBittorrent.Client;
using Category = Common.Configuration.DownloadCleaner.Category;

namespace Infrastructure.Verticals.DownloadClient.QBittorrent;

public class QBitService : DownloadService, IQBitService
{
    private readonly QBitConfig _config;
    private readonly QBittorrentClient _client;

    public QBitService(
        ILogger<QBitService> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<QBitConfig> config,
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
        _config = config.Value;
        _config.Validate();
        UriBuilder uriBuilder = new(_config.Url);
        uriBuilder.Path = string.IsNullOrEmpty(_config.UrlBase)
            ? uriBuilder.Path
            : $"{uriBuilder.Path.TrimEnd('/')}/{_config.UrlBase.TrimStart('/')}";
        _client = new(httpClientFactory.CreateClient(Constants.HttpClientWithRetryName), uriBuilder.Uri);
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
    public override async Task<DownloadCheckResult> ShouldRemoveFromArrQueueAsync(string hash, IReadOnlyList<string> ignoredDownloads)
    {
        DownloadCheckResult result = new();
        TorrentInfo? download = (await _client.GetTorrentListAsync(new TorrentListQuery { Hashes = [hash] }))
            .FirstOrDefault();

        if (download is null)
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
            return result;
        }

        IReadOnlyList<TorrentTracker> trackers = await GetTrackersAsync(hash);
        
        if (ignoredDownloads.Count > 0 &&
            (download.ShouldIgnore(ignoredDownloads) || trackers.Any(x => x.ShouldIgnore(ignoredDownloads)) is true))
        {
            _logger.LogInformation("skip | download is ignored | {name}", download.Name);
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

        IReadOnlyList<TorrentContent>? files = await _client.GetTorrentContentsAsync(hash);

        if (files?.Count is > 0 && files.All(x => x.Priority is TorrentContentPriority.Skip))
        {
            result.ShouldRemove = true;
            
            // if all files were blocked by qBittorrent
            if (download is { CompletionOn: not null, Downloaded: null or 0 })
            {
                result.DeleteReason = DeleteReason.AllFilesSkippedByQBit;
                return result;
            }
            
            // remove if all files are unwanted
            result.DeleteReason = DeleteReason.AllFilesSkipped;
            return result;
        }

        (result.ShouldRemove, result.DeleteReason) = await EvaluateDownloadRemoval(download, result.IsPrivate);

        return result;
    }

    /// <inheritdoc/>
    public override async Task<BlockFilesResult> BlockUnwantedFilesAsync(string hash,
        BlocklistType blocklistType,
        ConcurrentBag<string> patterns,
        ConcurrentBag<Regex> regexes,
        IReadOnlyList<string> ignoredDownloads
    )
    {
        TorrentInfo? download = (await _client.GetTorrentListAsync(new TorrentListQuery { Hashes = [hash] }))
            .FirstOrDefault();
        BlockFilesResult result = new();

        if (download is null)
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
            return result;
        }
        
        IReadOnlyList<TorrentTracker> trackers = await GetTrackersAsync(hash);
        
        if (ignoredDownloads.Count > 0 &&
            (download.ShouldIgnore(ignoredDownloads) || trackers.Any(x => x.ShouldIgnore(ignoredDownloads)) is true))
        {
            _logger.LogInformation("skip | download is ignored | {name}", download.Name);
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
            _logger.LogDebug("skip files check | download is private | {name}", download.Name);
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
            await _dryRunInterceptor.InterceptAsync(SkipFile, hash, fileIndex);
        }
        
        return result;
    }
    
    /// <inheritdoc/>
    public override async Task<List<object>?> GetAllDownloadsToBeCleaned(List<Category> categories) =>
        (await _client.GetTorrentListAsync(new()
        {
            Filter = TorrentListFilter.Seeding
        }))
        ?.Where(x => !string.IsNullOrEmpty(x.Hash))
        .Where(x => categories.Any(cat => cat.Name.Equals(x.Category, StringComparison.InvariantCultureIgnoreCase)))
        .Cast<object>()
        .ToList();

    /// <inheritdoc/>
    public override async Task CleanDownloads(List<object> downloads, List<Category> categoriesToClean, HashSet<string> excludedHashes,
        IReadOnlyList<string> ignoredDownloads)
    {
        foreach (TorrentInfo download in downloads)
        {
            if (string.IsNullOrEmpty(download.Hash))
            {
                continue;
            }
            
            IReadOnlyList<TorrentTracker> trackers = await GetTrackersAsync(download.Hash);

            if (ignoredDownloads.Count > 0 &&
                (download.ShouldIgnore(ignoredDownloads) || trackers.Any(x => x.ShouldIgnore(ignoredDownloads)) is true))
            {
                _logger.LogInformation("skip | download is ignored | {name}", download.Name);
                continue;
            }
            
            Category? category = categoriesToClean
                .FirstOrDefault(x => download.Category.Equals(x.Name, StringComparison.InvariantCultureIgnoreCase));
            
            if (category is null)
            {
                continue;
            }
            
            if (excludedHashes.Any(x => x.Equals(download.Hash, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogDebug("skip | download is used by an arr | {name}", download.Name);
                continue;
            }
            
            if (!_downloadCleanerConfig.DeletePrivate)
            {
                TorrentProperties? torrentProperties = await _client.GetTorrentPropertiesAsync(download.Hash);

                if (torrentProperties is null)
                {
                    _logger.LogDebug("failed to find torrent properties in the download client | {name}", download.Name);
                    return;
                }
                
                bool isPrivate = torrentProperties.AdditionalData.TryGetValue("is_private", out var dictValue) &&
                                 bool.TryParse(dictValue?.ToString(), out bool boolValue)
                                 && boolValue;

                if (isPrivate)
                {
                    _logger.LogDebug("skip | download is private | {name}", download.Name);
                    continue;
                }
            }
            
            ContextProvider.Set("downloadName", download.Name);
            ContextProvider.Set("hash", download.Hash);

            SeedingCheckResult result = ShouldCleanDownload(download.Ratio, download.SeedingTime ?? TimeSpan.Zero, category);

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
            
            await _notifier.NotifyDownloadCleaned(download.Ratio, download.SeedingTime ?? TimeSpan.Zero, category.Name, result.Reason);
        }
    }

    /// <inheritdoc/>
    [DryRunSafeguard]
    public override async Task DeleteDownload(string hash)
    {
        await _client.DeleteAsync(hash, deleteDownloadedData: true);
    }
    
    [DryRunSafeguard]
    protected virtual async Task SkipFile(string hash, int fileIndex)
    {
        await _client.SetFilePriorityAsync(hash, fileIndex, TorrentContentPriority.Skip);
    }

    public override void Dispose()
    {
        _client.Dispose();
    }

    private async Task<(bool, DeleteReason)> EvaluateDownloadRemoval(TorrentInfo torrent, bool isPrivate)
    {
        (bool ShouldRemove, DeleteReason Reason) result = await CheckIfSlow(torrent, isPrivate);

        if (result.ShouldRemove)
        {
            return result;
        }

        return await CheckIfStuck(torrent, isPrivate);
    }

    private async Task<(bool ShouldRemove, DeleteReason Reason)> CheckIfSlow(TorrentInfo download, bool isPrivate)
    {
        if (_queueCleanerConfig.SlowMaxStrikes is 0)
        {
            return (false, DeleteReason.None);
        }
        
        if (download.State is not (TorrentState.Downloading or TorrentState.ForcedDownload))
        {
            return (false, DeleteReason.None);
        }
        
        if (download.DownloadSpeed <= 0)
        {
            return (false, DeleteReason.None);
        }
        
        if (_queueCleanerConfig.SlowIgnorePrivate && isPrivate)
        {
            // ignore private trackers
            _logger.LogDebug("skip slow check | download is private | {name}", download.Name);
            return (false, DeleteReason.None);
        }

        if (download.Size > (_queueCleanerConfig.SlowIgnoreAboveSizeByteSize?.Bytes ?? long.MaxValue))
        {
            _logger.LogDebug("skip slow check | download is too large | {name}", download.Name);
            return (false, DeleteReason.None);
        }
        
        ByteSize minSpeed = _queueCleanerConfig.SlowMinSpeedByteSize;
        ByteSize currentSpeed = new ByteSize(download.DownloadSpeed);
        SmartTimeSpan maxTime = SmartTimeSpan.FromHours(_queueCleanerConfig.SlowMaxTime);
        SmartTimeSpan currentTime = new SmartTimeSpan(download.EstimatedTime ?? TimeSpan.Zero);

        return await CheckIfSlow(
            download.Hash,
            download.Name,
            minSpeed,
            currentSpeed,
            maxTime,
            currentTime
        );
    }

    private async Task<(bool ShouldRemove, DeleteReason Reason)> CheckIfStuck(TorrentInfo torrent, bool isPrivate)
    {
        if (_queueCleanerConfig.StalledMaxStrikes is 0)
        {
            return (false, DeleteReason.None);
        }
        
        if (torrent.State is not TorrentState.StalledDownload and not TorrentState.FetchingMetadata
            and not TorrentState.ForcedFetchingMetadata)
        {
            // ignore other states
            return (false, DeleteReason.None);
        }
        
        if (_queueCleanerConfig.StalledIgnorePrivate && isPrivate)
        {
            // ignore private trackers
            _logger.LogDebug("skip stalled check | download is private | {name}", torrent.Name);
            return (false, DeleteReason.None);
        }
        
        if (torrent.State is TorrentState.StalledDownload)
        {
            _logger.LogTrace("stalled download | {name}", torrent.Name);
            
            ResetStalledStrikesOnProgress(torrent.Hash, torrent.Downloaded ?? 0);
            
            return (await _striker.StrikeAndCheckLimit(torrent.Hash, torrent.Name, _queueCleanerConfig.StalledMaxStrikes, StrikeType.Stalled), DeleteReason.Stalled);
        }

        _logger.LogTrace("downloading metadata | {name}", torrent.Name);
        
        return (await _striker.StrikeAndCheckLimit(torrent.Hash, torrent.Name, _queueCleanerConfig.StalledMaxStrikes, StrikeType.DownloadingMetadata), DeleteReason.DownloadingMetadata);
    }

    private async Task<IReadOnlyList<TorrentTracker>> GetTrackersAsync(string hash)
    {
        return (await _client.GetTorrentTrackersAsync(hash))
            .Where(x => x.Url.Contains("**"))
            .ToList();
    }
}