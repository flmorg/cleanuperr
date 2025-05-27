using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Attributes;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.QueueCleaner;
using Common.CustomDataTypes;
using Common.Helpers;
using Data.Enums;
using Infrastructure.Configuration;
using Infrastructure.Extensions;
using Infrastructure.Http;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.Files;
using Infrastructure.Verticals.ItemStriker;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QBittorrent.Client;
using Infrastructure.Events;

namespace Infrastructure.Verticals.DownloadClient.QBittorrent;

public class QBitService : DownloadService, IQBitService
{
    protected QBittorrentClient? _client;

    public QBitService(
        ILogger<QBitService> logger,
        IHttpClientFactory httpClientFactory,
        IConfigManager configManager,
        IMemoryCache cache,
        IFilenameEvaluator filenameEvaluator,
        IStriker striker,
        INotificationPublisher notifier,
        IDryRunInterceptor dryRunInterceptor,
        IHardLinkFileService hardLinkFileService,
        IDynamicHttpClientProvider httpClientProvider,
        EventPublisher eventPublisher
    ) : base(
        logger, configManager, cache, filenameEvaluator, striker, notifier, dryRunInterceptor, hardLinkFileService,
        httpClientProvider, eventPublisher
    )
    {
        // Client will be initialized when Initialize() is called with a specific client configuration
    }
    
    /// <inheritdoc />
    public override void Initialize(ClientConfig clientConfig)
    {
        // Initialize base service first
        base.Initialize(clientConfig);
        
        // Ensure client type is correct
        if (clientConfig.Type != Common.Enums.DownloadClientType.QBittorrent)
        {
            throw new InvalidOperationException($"Cannot initialize QBitService with client type {clientConfig.Type}");
        }
        
        // Create QBittorrent client
        _client = new QBittorrentClient(_httpClient, clientConfig.Url);
        
        _logger.LogInformation("Initialized QBittorrent service for client {clientName} ({clientId})", 
            clientConfig.Name, clientConfig.Id);
    }

    public override async Task LoginAsync()
    {
        if (_client == null)
        {
            throw new InvalidOperationException("QBittorrent client is not initialized");
        }
        
        if (string.IsNullOrEmpty(_clientConfig.Username) && string.IsNullOrEmpty(_clientConfig.Password))
        {
            _logger.LogDebug("No credentials configured for client {clientId}, skipping login", _clientConfig.Id);
            return;
        }

        try
        {
            await _client.LoginAsync(_clientConfig.Username, _clientConfig.Password);
            _logger.LogDebug("Successfully logged in to QBittorrent client {clientId}", _clientConfig.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to login to QBittorrent client {clientId}", _clientConfig.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async Task<DownloadCheckResult> ShouldRemoveFromArrQueueAsync(string hash, IReadOnlyList<string> ignoredDownloads)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("QBittorrent client is not initialized");
        }
        
        DownloadCheckResult result = new();
        TorrentInfo? download = (await _client.GetTorrentListAsync(new TorrentListQuery { Hashes = [hash] }))
            .FirstOrDefault();

        if (download is null)
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
            return result;
        }

        result.Found = true;

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
    public override async Task<BlockFilesResult> BlockUnwantedFilesAsync(
        string hash,
        BlocklistType blocklistType,
        ConcurrentBag<string> patterns,
        ConcurrentBag<Regex> regexes,
        IReadOnlyList<string> ignoredDownloads
    )
    {
        if (_client == null)
        {
            throw new InvalidOperationException("QBittorrent client is not initialized");
        }
        
        BlockFilesResult result = new();
        TorrentInfo? download = (await _client.GetTorrentListAsync(new TorrentListQuery { Hashes = [hash] }))
            .FirstOrDefault();

        if (download is null)
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
            return result;
        }

        // Mark as processed since we found the download
        result.Found = true;

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
    public override async Task<List<object>?> GetSeedingDownloads()
    {
        if (_client == null)
        {
            throw new InvalidOperationException("QBittorrent client is not initialized");
        }
        
        var torrentList = await _client.GetTorrentListAsync(new TorrentListQuery { Filter = TorrentListFilter.Seeding });
        return torrentList?.Where(x => !string.IsNullOrEmpty(x.Hash))
            .Cast<object>()
            .ToList();
    }

    /// <inheritdoc/>
    public override List<object>? FilterDownloadsToBeCleanedAsync(List<object>? downloads, List<CleanCategory> categories) =>
        downloads
            ?.Cast<TorrentInfo>()
            .Where(x => !string.IsNullOrEmpty(x.Hash))
            .Where(x => categories.Any(cat => cat.Name.Equals(x.Category, StringComparison.InvariantCultureIgnoreCase)))
            .Cast<object>()
            .ToList();

    /// <inheritdoc/>
    public override List<object>? FilterDownloadsToChangeCategoryAsync(List<object>? downloads, List<string> categories) =>
        downloads
            ?.Cast<TorrentInfo>()
            .Where(x => !string.IsNullOrEmpty(x.Hash))
            .Where(x => categories.Any(cat => cat.Equals(x.Category, StringComparison.InvariantCultureIgnoreCase)))
            .Where(x =>
            {
                if (_downloadCleanerConfig.UnlinkedUseTag)
                {
                    return !x.Tags.Any(tag => tag.Equals(_downloadCleanerConfig.UnlinkedTargetCategory, StringComparison.InvariantCultureIgnoreCase));
                }

                return true;
            })
            .Cast<object>()
            .ToList();

    /// <inheritdoc/>
    public override async Task CleanDownloadsAsync(List<object>? downloads, List<CleanCategory> categoriesToClean,
        HashSet<string> excludedHashes, IReadOnlyList<string> ignoredDownloads)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("QBittorrent client is not initialized");
        }
        
        if (downloads?.Count is null or 0)
        {
            return;
        }

        foreach (TorrentInfo download in downloads)
        {
            if (string.IsNullOrEmpty(download.Hash))
            {
                continue;
            }

            if (excludedHashes.Any(x => x.Equals(download.Hash, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogDebug("skip | download is used by an arr | {name}", download.Name);
                continue;
            }

            IReadOnlyList<TorrentTracker> trackers = await GetTrackersAsync(download.Hash);

            if (ignoredDownloads.Count > 0 &&
                (download.ShouldIgnore(ignoredDownloads) || trackers.Any(x => x.ShouldIgnore(ignoredDownloads))))
            {
                _logger.LogInformation("skip | download is ignored | {name}", download.Name);
                continue;
            }

            CleanCategory? category = categoriesToClean
                .FirstOrDefault(x => download.Category.Equals(x.Name, StringComparison.InvariantCultureIgnoreCase));

            if (category is null)
            {
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

            await _eventPublisher.PublishDownloadCleaned(download.Ratio, download.SeedingTime ?? TimeSpan.Zero, category.Name, result.Reason);
        }
    }

    public override async Task CreateCategoryAsync(string name)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("QBittorrent client is not initialized");
        }
        
        IReadOnlyDictionary<string, Category>? existingCategories = await _client.GetCategoriesAsync();

        if (existingCategories.Any(x => x.Value.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
        {
            return;
        }

        await _dryRunInterceptor.InterceptAsync(CreateCategory, name);
    }

    public override async Task ChangeCategoryForNoHardLinksAsync(List<object>? downloads, HashSet<string> excludedHashes, IReadOnlyList<string> ignoredDownloads)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("QBittorrent client is not initialized");
        }
        
        if (downloads?.Count is null or 0)
        {
            return;
        }

        if (!string.IsNullOrEmpty(_downloadCleanerConfig.UnlinkedIgnoredRootDir))
        {
            _hardLinkFileService.PopulateFileCounts(_downloadCleanerConfig.UnlinkedIgnoredRootDir);
        }

        foreach (TorrentInfo download in downloads)
        {
            if (string.IsNullOrEmpty(download.Hash))
            {
                continue;
            }

            if (excludedHashes.Any(x => x.Equals(download.Hash, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogDebug("skip | download is used by an arr | {name}", download.Name);
                continue;
            }

            IReadOnlyList<TorrentTracker> trackers = await GetTrackersAsync(download.Hash);

            if (ignoredDownloads.Count > 0 &&
                (download.ShouldIgnore(ignoredDownloads) || trackers.Any(x => x.ShouldIgnore(ignoredDownloads))))
            {
                _logger.LogInformation("skip | download is ignored | {name}", download.Name);
                continue;
            }

            IReadOnlyList<TorrentContent>? files = await _client.GetTorrentContentsAsync(download.Hash);

            if (files is null)
            {
                _logger.LogDebug("failed to find files for {name}", download.Name);
                continue;
            }

            ContextProvider.Set("downloadName", download.Name);
            ContextProvider.Set("hash", download.Hash);
            bool hasHardlinks = false;

            foreach (TorrentContent file in files)
            {
                if (!file.Index.HasValue)
                {
                    _logger.LogDebug("skip | file index is null for {name}", download.Name);
                    hasHardlinks = true;
                    break;
                }

                string filePath = string.Join(Path.DirectorySeparatorChar, Path.Combine(download.SavePath, file.Name).Split(['\\', '/']));

                if (file.Priority is TorrentContentPriority.Skip)
                {
                    _logger.LogDebug("skip | file is not downloaded | {file}", filePath);
                    continue;
                }

                long hardlinkCount = _hardLinkFileService.GetHardLinkCount(filePath, !string.IsNullOrEmpty(_downloadCleanerConfig.UnlinkedIgnoredRootDir));

                if (hardlinkCount < 0)
                {
                    _logger.LogDebug("skip | could not get file properties | {file}", filePath);
                    hasHardlinks = true;
                    break;
                }

                if (hardlinkCount > 0)
                {
                    hasHardlinks = true;
                    break;
                }
            }

            if (hasHardlinks)
            {
                _logger.LogDebug("skip | download has hardlinks | {name}", download.Name);
                continue;
            }

            await _dryRunInterceptor.InterceptAsync(ChangeCategory, download.Hash, _downloadCleanerConfig.UnlinkedTargetCategory);

            if (_downloadCleanerConfig.UnlinkedUseTag)
            {
                _logger.LogInformation("tag added for {name}", download.Name);
            }
            else
            {
                _logger.LogInformation("category changed for {name}", download.Name);
                download.Category = _downloadCleanerConfig.UnlinkedTargetCategory;
            }

            await _eventPublisher.PublishCategoryChanged(download.Category, _downloadCleanerConfig.UnlinkedTargetCategory, _downloadCleanerConfig.UnlinkedUseTag);
        }
    }

    /// <inheritdoc/>
    [DryRunSafeguard]
    public override async Task DeleteDownload(string hash)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("QBittorrent client is not initialized");
        }
        
        await _client.DeleteAsync([hash], deleteDownloadedData: true);
    }

    [DryRunSafeguard]
    protected async Task CreateCategory(string name)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("QBittorrent client is not initialized");
        }
        
        await _client.AddCategoryAsync(name);
    }

    [DryRunSafeguard]
    protected virtual async Task SkipFile(string hash, int fileIndex)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("QBittorrent client is not initialized");
        }
        
        await _client.SetFilePriorityAsync(hash, fileIndex, TorrentContentPriority.Skip);
    }

    [DryRunSafeguard]
    protected virtual async Task ChangeCategory(string hash, string newCategory)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("QBittorrent client is not initialized");
        }
        
        if (_downloadCleanerConfig.UnlinkedUseTag)
        {
            await _client.AddTorrentTagAsync([hash], newCategory);
            return;
        }

        await _client.SetTorrentCategoryAsync([hash], newCategory);
    }

    public override void Dispose()
    {
        _client?.Dispose();
        _httpClient?.Dispose();
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
        if (_queueCleanerConfig.StalledMaxStrikes is 0 && _queueCleanerConfig.DownloadingMetadataMaxStrikes is 0)
        {
            return (false, DeleteReason.None);
        }

        if (torrent.State is not TorrentState.StalledDownload and not TorrentState.FetchingMetadata
            and not TorrentState.ForcedFetchingMetadata)
        {
            // ignore other states
            return (false, DeleteReason.None);
        }

        if (_queueCleanerConfig.StalledMaxStrikes > 0 && torrent.State is TorrentState.StalledDownload)
        {
            if (_queueCleanerConfig.StalledIgnorePrivate && isPrivate)
            {
                // ignore private trackers
                _logger.LogDebug("skip stalled check | download is private | {name}", torrent.Name);
            }
            else
            {
                ResetStalledStrikesOnProgress(torrent.Hash, torrent.Downloaded ?? 0);
            
                return (await _striker.StrikeAndCheckLimit(torrent.Hash, torrent.Name, _queueCleanerConfig.StalledMaxStrikes, StrikeType.Stalled), DeleteReason.Stalled);
            }
        }

        if (_queueCleanerConfig.DownloadingMetadataMaxStrikes > 0 && torrent.State is not TorrentState.StalledDownload)
        {
            return (await _striker.StrikeAndCheckLimit(torrent.Hash, torrent.Name, _queueCleanerConfig.DownloadingMetadataMaxStrikes, StrikeType.DownloadingMetadata), DeleteReason.DownloadingMetadata);
        }

        return (false, DeleteReason.None);
    }

    private async Task<IReadOnlyList<TorrentTracker>> GetTrackersAsync(string hash)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("QBittorrent client is not initialized");
        }
        
        return (await _client.GetTorrentTrackersAsync(hash))
            .Where(x => x.Url.Contains("**"))
            .ToList();
    }
}