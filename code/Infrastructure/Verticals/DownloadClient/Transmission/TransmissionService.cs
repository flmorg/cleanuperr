using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Attributes;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.QueueCleaner;
using Common.Helpers;
using Domain.Enums;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.Context;
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
        IDryRunInterceptor dryRunInterceptor
    ) : base(
        logger, queueCleanerConfig, contentBlockerConfig, downloadCleanerConfig, cache,
        filenameEvaluator, striker, notifier, dryRunInterceptor
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

    /// <inheritdoc/>
    public override async Task<List<object>?> GetAllDownloadsToBeCleaned(List<Category> categories)
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

    /// <inheritdoc/>
    public override async Task CleanDownloads(List<object> downloads, List<Category> categoriesToClean, HashSet<string> excludedHashes)
    {
        foreach (TorrentInfo download in downloads)
        {
            if (string.IsNullOrEmpty(download.HashString))
            {
                continue;
            }
            
            Category? category = categoriesToClean
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