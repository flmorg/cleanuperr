using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Attributes;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.QueueCleaner;
using Common.Exceptions;
using Common.Helpers;
using Domain.Enums;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.Files;
using Infrastructure.Verticals.ItemStriker;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QBittorrent.Client;

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
        IDryRunInterceptor dryRunInterceptor,
        IHardlinkFileService hardlinkFileService
    ) : base(
        logger, queueCleanerConfig, contentBlockerConfig, downloadCleanerConfig, cache,
        filenameEvaluator, striker, notifier, dryRunInterceptor, hardlinkFileService
    )
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
            await _dryRunInterceptor.InterceptAsync(SkipFile, hash, fileIndex);
        }
        
        return result;
    }
    
    /// <inheritdoc/>
    public override async Task<List<object>?> GetDownloadsToBeCleanedAsync(List<CleanCategory> categories) =>
        (await _client.GetTorrentListAsync(new()
        {
            Filter = TorrentListFilter.Seeding
        }))
        ?.Where(x => !string.IsNullOrEmpty(x.Hash))
        .Where(x => categories.Any(cat => cat.Name.Equals(x.Category, StringComparison.InvariantCultureIgnoreCase)))
        .Cast<object>()
        .ToList();

    public override async Task<List<object>?> GetDownloadsToChangeCategoryAsync(List<string> categories)
    {
        return (await _client.GetTorrentListAsync(new()
            {
                Filter = TorrentListFilter.Seeding
            }))
            ?.Where(x => !string.IsNullOrEmpty(x.Hash))
            .Cast<object>()
            .ToList();
    }

    /// <inheritdoc/>
    public override async Task CleanDownloadsAsync(List<object> downloads, List<CleanCategory> categoriesToClean, HashSet<string> excludedHashes)
    {
        foreach (TorrentInfo download in downloads)
        {
            if (string.IsNullOrEmpty(download.Hash))
            {
                continue;
            }
            
            CleanCategory? category = categoriesToClean
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

    public override async Task CreateCategoryAsync(string name)
    {
        IReadOnlyDictionary<string, Category>? existingCategories = await _client.GetCategoriesAsync();
        
        if (existingCategories.Any(x => x.Value.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
        {
            return;
        }
        
        await _client.AddCategoryAsync(name);
    }

    public override async Task ChangeCategoryForNoHardLinksAsync(List<object> downloads, HashSet<string> excludedHashes)
    {
        if (_downloadCleanerConfig.IgnoreRootDir)
        {
            downloads
                .Cast<TorrentInfo>()
                .GroupBy(x => Path.GetPathRoot(x.SavePath))
                .Select(x => x.Key)
                .ToList()
                .ForEach(x =>
                {
                    _logger.LogTrace("populating file counts from {dir}", x);
                    
                    if (!Directory.Exists(x))
                    {
                        throw new ValidationException($"directory \"{x}\" does not exist");
                    }
                    
                    _hardLinkFileService.PopulateFileCounts(x);
                });
        }
        
        foreach (TorrentInfo download in downloads)
        {
            IReadOnlyList<TorrentContent>? files = await _client.GetTorrentContentsAsync(download.Hash);

            if (files is null)
            {
                _logger.LogDebug("failed to find files for {name}", download.Name);
                return;
            }
            
            if (excludedHashes.Any(x => x.Equals(download.Hash, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogDebug("skip | download is used by an arr | {name}", download.Name);
                continue;
            }

            bool hasHardlinks = false;
            
            foreach (TorrentContent file in files)
            {
                if (!file.Index.HasValue)
                {
                    _logger.LogDebug("skip | file index is null for {name}", download.Name);
                    return;
                }

                string filePath = Path.Combine(Directory.Exists(download.ContentPath)
                    ? download.ContentPath
                    : download.SavePath, file.Name
                );

                long hardlinkCount = _hardLinkFileService.GetHardLinkCount(filePath, _downloadCleanerConfig.IgnoreRootDir);

                if (hardlinkCount < 0)
                {
                    _logger.LogDebug("skip | could not get file properties | {name}", download.Name);
                    hasHardlinks = true;
                    break;
                }

                if (hardlinkCount > 1)
                {
                    hasHardlinks = true;
                }
            }
            
            if (hasHardlinks)
            {
                _logger.LogDebug("skip | download has hardlinks | {name}", download.Name);
                continue;
            }
            
            _logger.LogInformation("changing category for {name}", download.Name);
            
            await ((QBitService)Proxy).ChangeCategory(download.Hash, _downloadCleanerConfig.NoHardlinksCategory);
            await _notifier.NotifyCategoryChanged(download.Category, _downloadCleanerConfig.NoHardlinksCategory);
        }
    }
    
    /// <inheritdoc/>
    [DryRunSafeguard]
    public override async Task DeleteDownloadAsync(string hash)
    {
        await _client.DeleteAsync(hash, deleteDownloadedData: true);
    }
    
    [DryRunSafeguard]
    protected virtual async Task SkipFile(string hash, int fileIndex)
    {
        await _client.SetFilePriorityAsync(hash, fileIndex, TorrentContentPriority.Skip);
    }

    [DryRunSafeguard]
    protected virtual async Task ChangeCategory(string hash, string newCategory)
    {
        await _client.SetTorrentCategoryAsync([hash], newCategory);
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