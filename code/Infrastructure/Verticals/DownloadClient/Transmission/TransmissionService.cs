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
using Transmission.API.RPC;
using Transmission.API.RPC.Arguments;
using Transmission.API.RPC.Entity;

namespace Infrastructure.Verticals.DownloadClient.Transmission;

public class TransmissionService : DownloadService, ITransmissionService
{
    private readonly TransmissionConfig _config;
    private readonly Client _client;
    private TorrentInfo[]? _torrentsCache;

    public TransmissionService(
        IHttpClientFactory httpClientFactory,
        ILogger<TransmissionService> logger,
        IOptions<TransmissionConfig> config,
        IOptions<QueueCleanerConfig> queueCleanerConfig,
        IOptions<ContentBlockerConfig> contentBlockerConfig,
        IOptions<DownloadCleanerConfig> downloadCleanerConfig,
        IMemoryCache cache,
        IFilenameEvaluator filenameEvaluator,
        IStriker striker,
        INotificationPublisher notifier,
        IDryRunInterceptor dryRunInterceptor,
        IHardLinkFileService hardLinkFileService
    ) : base(
        logger, queueCleanerConfig, contentBlockerConfig, downloadCleanerConfig, cache,
        filenameEvaluator, striker, notifier, dryRunInterceptor, hardLinkFileService
    )
    {
        _config = config.Value;
        _config.Validate();
        _client = new(
            httpClientFactory.CreateClient(Constants.HttpClientWithRetryName),
            new Uri(_config.Url, "/transmission/rpc").ToString(),
            login: _config.Username,
            password: _config.Password
        );
    }

    public override async Task LoginAsync()
    {
        await _client.GetSessionInformationAsync();
    }

    /// <inheritdoc/>
    public override async Task<StalledResult> ShouldRemoveFromArrQueueAsync(string hash)
    {
        StalledResult result = new();
        TorrentInfo? torrent = await GetTorrentAsync(hash);

        if (torrent is null)
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
            return result;
        }
        
        bool shouldRemove = torrent.FileStats?.Length > 0;
        result.IsPrivate = torrent.IsPrivate ?? false;

        foreach (TransmissionTorrentFileStats? stats in torrent.FileStats ?? [])
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
            result.DeleteReason = DeleteReason.AllFilesBlocked;
        }

        // remove if all files are unwanted or download is stuck
        result.ShouldRemove = shouldRemove || await IsItemStuckAndShouldRemove(torrent);

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
        TorrentInfo? torrent = await GetTorrentAsync(hash);
        BlockFilesResult result = new();

        if (torrent?.FileStats is null || torrent.Files is null)
        {
            return result;
        }

        bool isPrivate = torrent.IsPrivate ?? false;
        result.IsPrivate = isPrivate;
        
        if (_contentBlockerConfig.IgnorePrivate && isPrivate)
        {
            // ignore private trackers
            _logger.LogDebug("skip files check | download is private | {name}", torrent.Name);
            return result;
        }

        List<long> unwantedFiles = [];
        long totalFiles = 0;
        long totalUnwantedFiles = 0;
        
        for (int i = 0; i < torrent.Files.Length; i++)
        {
            if (torrent.FileStats?[i].Wanted == null)
            {
                continue;
            }

            totalFiles++;
            
            if (!torrent.FileStats[i].Wanted.Value)
            {
                totalUnwantedFiles++;
                continue;
            }

            if (_filenameEvaluator.IsValid(torrent.Files[i].Name, blocklistType, patterns, regexes))
            {
                continue;
            }
            
            _logger.LogInformation("unwanted file found | {file}", torrent.Files[i].Name);
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

        await _dryRunInterceptor.InterceptAsync(SetUnwantedFiles, torrent.Id, unwantedFiles.ToArray());

        return result;
    }
    
    public override async Task<List<object>?> GetSeedingDownloads()
    {
        string[] fields = [
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
            TorrentFields.UPLOAD_RATIO
        ];

        return (await _client.TorrentGetAsync(fields))
            ?.Torrents
            ?.Where(x => !string.IsNullOrEmpty(x.HashString))
            .Where(x => x.Status is 5 or 6)
            .Cast<object>()
            .ToList();
    }

    /// <inheritdoc/>
    public override List<object>? FilterDownloadsToBeCleanedAsync(List<object>? downloads, List<CleanCategory> categories)
    {
        return downloads
            ?
            .Cast<TorrentInfo>()
            .Where(x => categories
                .Any(cat =>
                {
                    if (x.DownloadDir is null)
                    {
                        return false;
                    }
                    
                    return Path.GetFileName(Path.TrimEndingDirectorySeparator(x.DownloadDir))
                        .Equals(cat.Name, StringComparison.InvariantCultureIgnoreCase);
                })
            )
            .Cast<object>()
            .ToList();
    }

    public override List<object>? FilterDownloadsToChangeCategoryAsync(List<object>? downloads, List<string> categories)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override async Task CleanDownloadsAsync(List<object>? downloads, List<CleanCategory> categoriesToClean,
        HashSet<string> excludedHashes)
    {
        foreach (TorrentInfo download in downloads)
        {
            if (string.IsNullOrEmpty(download.HashString))
            {
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

            if (excludedHashes.Any(x => x.Equals(download.HashString, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogDebug("skip | download is used by an arr | {name}", download.Name);
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
        throw new NotImplementedException();
    }

    public override async Task ChangeCategoryForNoHardLinksAsync(List<object>? downloads, HashSet<string> excludedHashes)
    {
        if (downloads?.Count is null or 0)
        {
            return;
        }
        
        if (_downloadCleanerConfig.NoHardLinksIgnoreRootDir)
        {
            downloads
                .Cast<TorrentInfo>()
                .Select(x =>
                {
                    if (x.DownloadDir == null)
                    {
                        return string.Empty;
                    }
                    
                    string? firstDir = GetRootWithFirstDirectory(x.DownloadDir);

                    if (string.IsNullOrEmpty(firstDir))
                    {
                        return string.Empty;
                    }

                    if (firstDir == Path.GetPathRoot(x.DownloadDir))
                    {
                        return string.Empty;
                    }
                    
                    return firstDir;
                })
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
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
        
        foreach (TorrentInfo download in downloads.Cast<TorrentInfo>())
        {
            if (string.IsNullOrEmpty(download.HashString) || download.DownloadDir == null)
            {
                _logger.LogDebug("skip | download hash or download directory is null for {name}", download.Name);
                continue;
            }
            
            if (excludedHashes.Any(x => x.Equals(download.HashString, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogDebug("skip | download is used by an arr | {name}", download.Name);
                continue;
            }

            ContextProvider.Set("downloadName", download.Name);
            ContextProvider.Set("hash", download.HashString);
            
            bool hasHardlinks = false;
            
            if (download.Files != null)
            {
                foreach (TransmissionTorrentFiles file in download.Files)
                {
                    string filePath = Path.Combine(download.DownloadDir, file.Name);
                    
                    long hardlinkCount = _hardLinkFileService.GetHardLinkCount(filePath, _downloadCleanerConfig.NoHardLinksIgnoreRootDir);

                    if (hardlinkCount < 0)
                    {
                        _logger.LogDebug("skip | could not get file properties | {name}", download.Name);
                        hasHardlinks = true;
                        break;
                    }

                    if (hardlinkCount > 0)
                    {
                        hasHardlinks = true;
                        break;
                    }
                }
            }
            
            if (hasHardlinks)
            {
                _logger.LogDebug("skip | download has hardlinks | {name}", download.Name);
                continue;
            }
            
            // Get the current category (directory name)
            string currentCategory = Path.GetFileName(Path.TrimEndingDirectorySeparator(download.DownloadDir));
            
            // Create the new location path
            string newLocation = Path.Combine(
                Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(download.DownloadDir)) ?? string.Empty,
                _downloadCleanerConfig.NoHardLinksCategory
            );
            
            await _dryRunInterceptor.InterceptAsync(MoveDownload, download.Id, newLocation);
            
            _logger.LogInformation("category changed for {name}", download.Name);
            
            await _notifier.NotifyCategoryChanged(currentCategory, _downloadCleanerConfig.NoHardLinksCategory);
        }
    }

    [DryRunSafeguard]
    protected virtual async Task MoveDownload(long downloadId, string newLocation)
    {
        await _client.TorrentSetAsync(new TorrentSettings
        {
            Ids = [downloadId],
            Location = newLocation,
            // Move = true
        });
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
    
    private async Task<bool> IsItemStuckAndShouldRemove(TorrentInfo torrent)
    {
        if (_queueCleanerConfig.StalledMaxStrikes is 0)
        {
            return false;
        }
        
        if (_queueCleanerConfig.StalledIgnorePrivate && (torrent.IsPrivate ?? false))
        {
            // ignore private trackers
            _logger.LogDebug("skip stalled check | download is private | {name}", torrent.Name);
            return false;
        }
        
        if (torrent.Status is not 4)
        {
            // not in downloading state
            return false;
        }

        if (torrent.Eta > 0)
        {
            return false;
        }
        
        ResetStrikesOnProgress(torrent.HashString!, torrent.DownloadedEver ?? 0);

        return await StrikeAndCheckLimit(torrent.HashString!, torrent.Name!);
    }

    private async Task<TorrentInfo?> GetTorrentAsync(string hash)
    {
        TorrentInfo? torrent = _torrentsCache?
            .FirstOrDefault(x => x.HashString.Equals(hash, StringComparison.InvariantCultureIgnoreCase));
        
        if (_torrentsCache is null || torrent is null)
        {
            string[] fields = [
                TorrentFields.FILES,
                TorrentFields.FILE_STATS,
                TorrentFields.HASH_STRING,
                TorrentFields.ID,
                TorrentFields.ETA,
                TorrentFields.NAME,
                TorrentFields.STATUS,
                TorrentFields.IS_PRIVATE,
                TorrentFields.DOWNLOADED_EVER
            ];
            
            // refresh cache
            _torrentsCache = (await _client.TorrentGetAsync(fields))
                ?.Torrents;
        }
        
        if (_torrentsCache?.Length is null or 0)
        {
            _logger.LogDebug("could not list torrents | {url}", _config.Url);
        }
        
        torrent = _torrentsCache?.FirstOrDefault(x => x.HashString.Equals(hash, StringComparison.InvariantCultureIgnoreCase));

        if (torrent is null)
        {
            _logger.LogDebug("could not find torrent | {hash} | {url}", hash, _config.Url);
        }

        return torrent;
    }
}