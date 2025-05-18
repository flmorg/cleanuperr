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
using Infrastructure.Verticals.Files;
using Infrastructure.Verticals.ItemStriker;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Infrastructure.Configuration;
using Infrastructure.Http;
using Transmission.API.RPC;
using Transmission.API.RPC.Arguments;
using Transmission.API.RPC.Entity;

namespace Infrastructure.Verticals.DownloadClient.Transmission;

public class TransmissionService : DownloadService, ITransmissionService
{
    private Client? _client;

    private static readonly string[] Fields =
    [
        TorrentFields.FILES,
        TorrentFields.FILE_STATS,
        TorrentFields.HASH_STRING,
        TorrentFields.ID,
        TorrentFields.ETA,
        TorrentFields.NAME,
        TorrentFields.STATUS,
        TorrentFields.IS_PRIVATE,
        TorrentFields.DOWNLOADED_EVER,
        TorrentFields.DOWNLOAD_DIR,
        TorrentFields.SECONDS_SEEDING,
        TorrentFields.UPLOAD_RATIO,
        TorrentFields.TRACKERS,
        TorrentFields.RATE_DOWNLOAD,
        TorrentFields.TOTAL_SIZE,
    ];

    public TransmissionService(
        ILogger<TransmissionService> logger,
        IConfigManager configManager,
        IMemoryCache cache,
        IFilenameEvaluator filenameEvaluator,
        IStriker striker,
        INotificationPublisher notifier,
        IDryRunInterceptor dryRunInterceptor,
        IHardLinkFileService hardLinkFileService,
        IDynamicHttpClientProvider httpClientProvider
    ) : base(
        logger, configManager, cache,
        filenameEvaluator, striker, notifier, dryRunInterceptor, hardLinkFileService,
        httpClientProvider
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
        if (clientConfig.Type != Common.Enums.DownloadClientType.Transmission)
        {
            throw new InvalidOperationException($"Cannot initialize TransmissionService with client type {clientConfig.Type}");
        }
        
        if (_httpClient == null)
        {
            throw new InvalidOperationException("HTTP client is not initialized");
        }
        
        // Create the RPC path
        string rpcPath = string.IsNullOrEmpty(clientConfig.UrlBase)
            ? "/rpc"
            : $"/{clientConfig.UrlBase.TrimStart('/').TrimEnd('/')}/rpc";
        
        // Create full RPC URL
        string rpcUrl = new UriBuilder(clientConfig.Url) { Path = rpcPath }.Uri.ToString();
        
        // Create Transmission client
        _client = new Client(_httpClient, rpcUrl, login: clientConfig.Username, password: clientConfig.Password);
        
        _logger.LogInformation("Initialized Transmission service for client {clientName} ({clientId})", 
            clientConfig.Name, clientConfig.Id);
    }

    public override async Task LoginAsync()
    {
        if (_client == null)
        {
            throw new InvalidOperationException("Transmission client is not initialized");
        }
        
        try 
        {
            await _client.GetSessionInformationAsync();
            _logger.LogDebug("Successfully logged in to Transmission client {clientId}", _clientConfig.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to login to Transmission client {clientId}", _clientConfig.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async Task<DownloadCheckResult> ShouldRemoveFromArrQueueAsync(string hash, IReadOnlyList<string> ignoredDownloads)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("Transmission client is not initialized");
        }
        
        DownloadCheckResult result = new();
        TorrentInfo? download = await GetTorrentAsync(hash);

        if (download is null)
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
            return result;
        }
        
        result.Found = true;
        
        if (ignoredDownloads.Count > 0 && download.ShouldIgnore(ignoredDownloads))
        {
            _logger.LogDebug("skip | download is ignored | {name}", download.Name);
            return result;
        }
        
        bool shouldRemove = download.FileStats?.Length > 0;
        result.IsPrivate = download.IsPrivate ?? false;

        foreach (TransmissionTorrentFileStats? stats in download.FileStats ?? [])
        {
            if (!stats.Wanted.HasValue)
            {
                // if any files stats are missing, do not remove
                shouldRemove = false;
            }
            
            if (stats.Wanted.HasValue && stats.Wanted.Value)
            {
                // if any files are wanted, do not remove
                shouldRemove = false;
            }
        }
        
        if (shouldRemove)
        {
            // remove if all files are unwanted
            result.ShouldRemove = true;
            result.DeleteReason = DeleteReason.AllFilesBlocked;
            return result;
        }

        // remove if download is stuck
        (result.ShouldRemove, result.DeleteReason) = await EvaluateDownloadRemoval(download);

        return result;
    }

    /// <inheritdoc/>
    public override async Task<BlockFilesResult> BlockUnwantedFilesAsync(string hash,
        BlocklistType blocklistType,
        ConcurrentBag<string> patterns,
        ConcurrentBag<Regex> regexes, IReadOnlyList<string> ignoredDownloads)
    {
        TorrentInfo? download = await GetTorrentAsync(hash);
        BlockFilesResult result = new();

        if (download?.FileStats is null || download.Files is null)
        {
            return result;
        }
        
        // Mark as processed since we found the download
        result.Found = true;
        
        if (ignoredDownloads.Count > 0 && download.ShouldIgnore(ignoredDownloads))
        {
            _logger.LogDebug("skip | download is ignored | {name}", download.Name);
            return result;
        }

        bool isPrivate = download.IsPrivate ?? false;
        result.IsPrivate = isPrivate;
        
        if (_contentBlockerConfig.IgnorePrivate && isPrivate)
        {
            // ignore private trackers
            _logger.LogDebug("skip files check | download is private | {name}", download.Name);
            return result;
        }
        
        List<long> unwantedFiles = [];
        long totalFiles = 0;
        long totalUnwantedFiles = 0;
        
        for (int i = 0; i < download.Files.Length; i++)
        {
            if (download.FileStats?[i].Wanted == null)
            {
                continue;
            }

            totalFiles++;
            
            if (!download.FileStats[i].Wanted.Value)
            {
                totalUnwantedFiles++;
                continue;
            }

            if (_filenameEvaluator.IsValid(download.Files[i].Name, blocklistType, patterns, regexes))
            {
                continue;
            }
            
            _logger.LogInformation("unwanted file found | {file}", download.Files[i].Name);
            unwantedFiles.Add(i);
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
        
        _logger.LogDebug("changing priorities | torrent {hash}", hash);

        await _dryRunInterceptor.InterceptAsync(SetUnwantedFiles, download.Id, unwantedFiles.ToArray());

        return result;
    }
    
    public override async Task<List<object>?> GetSeedingDownloads() =>
        (await _client.TorrentGetAsync(Fields))
        ?.Torrents
        ?.Where(x => !string.IsNullOrEmpty(x.HashString))
        .Where(x => x.Status is 5 or 6)
        .Cast<object>()
        .ToList();

    /// <inheritdoc/>
    public override List<object>? FilterDownloadsToBeCleanedAsync(List<object>? downloads, List<CleanCategory> categories)
    {
        return downloads
            ?
            .Cast<TorrentInfo>()
            .Where(x => categories
                .Any(cat => cat.Name.Equals(x.GetCategory(), StringComparison.InvariantCultureIgnoreCase))
            )
            .Cast<object>()
            .ToList();
    }

    public override List<object>? FilterDownloadsToChangeCategoryAsync(List<object>? downloads, List<string> categories)
    {
        return downloads
            ?.Cast<TorrentInfo>()
            .Where(x => !string.IsNullOrEmpty(x.HashString))
            .Where(x => categories.Any(cat => cat.Equals(x.GetCategory(), StringComparison.InvariantCultureIgnoreCase)))
            .Cast<object>()
            .ToList();
    }

    /// <inheritdoc/>
    public override async Task CleanDownloadsAsync(List<object>? downloads, List<CleanCategory> categoriesToClean,
        HashSet<string> excludedHashes, IReadOnlyList<string> ignoredDownloads)
    {
        if (downloads?.Count is null or 0)
        {
            return;
        }
        
        foreach (TorrentInfo download in downloads)
        {
            if (string.IsNullOrEmpty(download.HashString))
            {
                continue;
            }
            
            if (excludedHashes.Any(x => x.Equals(download.HashString, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogDebug("skip | download is used by an arr | {name}", download.Name);
                continue;
            }

            if (ignoredDownloads.Count > 0 && download.ShouldIgnore(ignoredDownloads))
            {
                _logger.LogDebug("skip | download is ignored | {name}", download.Name);
                continue;
            }
            
            CleanCategory? category = categoriesToClean
                .FirstOrDefault(x =>
                {
                    if (download.DownloadDir is null)
                    {
                        return false;
                    }

                    return Path.GetFileName(Path.TrimEndingDirectorySeparator(download.DownloadDir))
                        .Equals(x.Name, StringComparison.InvariantCultureIgnoreCase);
                });
            
            if (category is null)
            {
                continue;
            }

            if (!_downloadCleanerConfig.DeletePrivate && download.IsPrivate is true)
            {
                _logger.LogDebug("skip | download is private | {name}", download.Name);
                continue;
            }
            
            ContextProvider.Set("downloadName", download.Name);
            ContextProvider.Set("hash", download.HashString);

            TimeSpan seedingTime = TimeSpan.FromSeconds(download.SecondsSeeding ?? 0);
            SeedingCheckResult result = ShouldCleanDownload(download.uploadRatio ?? 0, seedingTime, category);
            
            if (!result.ShouldClean)
            {
                continue;
            }

            await _dryRunInterceptor.InterceptAsync(RemoveDownloadAsync, download.Id);

            _logger.LogInformation(
                "download cleaned | {reason} reached | {name}",
                result.Reason is CleanReason.MaxRatioReached
                    ? "MAX_RATIO & MIN_SEED_TIME"
                    : "MAX_SEED_TIME",
                download.Name
            );

            await _notifier.NotifyDownloadCleaned(download.uploadRatio ?? 0, seedingTime, category.Name, result.Reason);
        }
    }
    
    public override async Task CreateCategoryAsync(string name)
    {
        await Task.CompletedTask;
    }

    public override async Task ChangeCategoryForNoHardLinksAsync(List<object>? downloads, HashSet<string> excludedHashes, IReadOnlyList<string> ignoredDownloads)
    {
        if (downloads?.Count is null or 0)
        {
            return;
        }
        
        if (!string.IsNullOrEmpty(_downloadCleanerConfig.UnlinkedIgnoredRootDir))
        {
            _hardLinkFileService.PopulateFileCounts(_downloadCleanerConfig.UnlinkedIgnoredRootDir);
        }
        
        foreach (TorrentInfo download in downloads.Cast<TorrentInfo>())
        {
            if (string.IsNullOrEmpty(download.HashString) || string.IsNullOrEmpty(download.Name) || download.DownloadDir == null)
            {
                continue;
            }
            
            if (excludedHashes.Any(x => x.Equals(download.HashString, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogDebug("skip | download is used by an arr | {name}", download.Name);
                continue;
            }
            
            if (ignoredDownloads.Count > 0 && download.ShouldIgnore(ignoredDownloads))
            {
                _logger.LogDebug("skip | download is ignored | {name}", download.Name);
                continue;
            }
        
            ContextProvider.Set("downloadName", download.Name);
            ContextProvider.Set("hash", download.HashString);
            
            bool hasHardlinks = false;
            
            if (download.Files is null || download.FileStats is null)
            {
                _logger.LogDebug("skip | download has no files | {name}", download.Name);
                continue;
            }

            for (int i = 0; i < download.Files.Length; i++)
            {
                TransmissionTorrentFiles file = download.Files[i];
                TransmissionTorrentFileStats stats = download.FileStats[i];
                
                if (stats.Wanted is null or false || string.IsNullOrEmpty(file.Name))
                {
                    continue;
                }

                string filePath = string.Join(Path.DirectorySeparatorChar, Path.Combine(download.DownloadDir, file.Name).Split(['\\', '/']));
                
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
            
            string currentCategory = download.GetCategory();
            string newLocation = string.Join(Path.DirectorySeparatorChar, Path.Combine(download.DownloadDir, _downloadCleanerConfig.UnlinkedTargetCategory).Split(['\\', '/']));
            
            await _dryRunInterceptor.InterceptAsync(ChangeDownloadLocation, download.Id, newLocation);
            
            _logger.LogInformation("category changed for {name}", download.Name);
            
            await _notifier.NotifyCategoryChanged(currentCategory, _downloadCleanerConfig.UnlinkedTargetCategory);

            download.DownloadDir = newLocation;
        }
    }

    [DryRunSafeguard]
    protected virtual async Task ChangeDownloadLocation(long downloadId, string newLocation)
    {
        await _client.TorrentSetLocationAsync([downloadId], newLocation, true);
    }

    public override async Task DeleteDownload(string hash)
    {
        TorrentInfo? torrent = await GetTorrentAsync(hash);

        if (torrent is null)
        {
            return;
        }

        await _client.TorrentRemoveAsync([torrent.Id], true);
    }

    public override void Dispose()
    {
        _client = null;
        _httpClient?.Dispose();
    }
    
    [DryRunSafeguard]
    protected virtual async Task RemoveDownloadAsync(long downloadId)
    {
        await _client.TorrentRemoveAsync([downloadId], true);
    }
    
    [DryRunSafeguard]
    protected virtual async Task SetUnwantedFiles(long downloadId, long[] unwantedFiles)
    {
        await _client.TorrentSetAsync(new TorrentSettings
        {
            Ids = [downloadId],
            FilesUnwanted = unwantedFiles,
        });
    }
    
    private async Task<(bool, DeleteReason)> EvaluateDownloadRemoval(TorrentInfo torrent)
    {
        (bool ShouldRemove, DeleteReason Reason) result = await CheckIfSlow(torrent);

        if (result.ShouldRemove)
        {
            return result;
        }

        return await CheckIfStuck(torrent);
    }

    private async Task<(bool ShouldRemove, DeleteReason Reason)> CheckIfSlow(TorrentInfo download)
    {
        if (_queueCleanerConfig.SlowMaxStrikes is 0)
        {
            return (false, DeleteReason.None);
        }
        
        if (download.Status is not 4)
        {
            // not in downloading state
            return (false, DeleteReason.None);
        }
        
        if (download.RateDownload <= 0)
        {
            return (false, DeleteReason.None);
        }
        
        if (_queueCleanerConfig.SlowIgnorePrivate && download.IsPrivate is true)
        {
            // ignore private trackers
            _logger.LogDebug("skip slow check | download is private | {name}", download.Name);
            return (false, DeleteReason.None);
        }

        if (download.TotalSize > (_queueCleanerConfig.SlowIgnoreAboveSizeByteSize?.Bytes ?? long.MaxValue))
        {
            _logger.LogDebug("skip slow check | download is too large | {name}", download.Name);
            return (false, DeleteReason.None);
        }
        
        ByteSize minSpeed = _queueCleanerConfig.SlowMinSpeedByteSize;
        ByteSize currentSpeed = new ByteSize(download.RateDownload ?? long.MaxValue);
        SmartTimeSpan maxTime = SmartTimeSpan.FromHours(_queueCleanerConfig.SlowMaxTime);
        SmartTimeSpan currentTime = SmartTimeSpan.FromSeconds(download.Eta ?? 0);

        return await CheckIfSlow(
            download.HashString!,
            download.Name!,
            minSpeed,
            currentSpeed,
            maxTime,
            currentTime
        );
    }

    private async Task<(bool ShouldRemove, DeleteReason Reason)> CheckIfStuck(TorrentInfo download)
    {
        if (_queueCleanerConfig.StalledMaxStrikes is 0)
        {
            return (false, DeleteReason.None);
        }
        
        if (download.Status is not 4)
        {
            // not in downloading state
            return (false, DeleteReason.None);
        }
        
        if (download.RateDownload > 0 || download.Eta > 0)
        {
            return (false, DeleteReason.None);
        }
        
        if (_queueCleanerConfig.StalledIgnorePrivate && (download.IsPrivate ?? false))
        {
            // ignore private trackers
            _logger.LogDebug("skip stalled check | download is private | {name}", download.Name);
            return (false, DeleteReason.None);
        }
        
        ResetStalledStrikesOnProgress(download.HashString!, download.DownloadedEver ?? 0);
        
        return (await _striker.StrikeAndCheckLimit(download.HashString!, download.Name!, _queueCleanerConfig.StalledMaxStrikes, StrikeType.Stalled), DeleteReason.Stalled);
    }

    private async Task<TorrentInfo?> GetTorrentAsync(string hash)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("Transmission client is not initialized");
        }
        
        return (await _client.TorrentGetAsync(Fields, hash))
            ?.Torrents
            ?.FirstOrDefault();
    }
}