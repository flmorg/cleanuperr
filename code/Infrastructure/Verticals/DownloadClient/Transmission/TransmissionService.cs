using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadClient;
using Common.Configuration.QueueCleaner;
using Common.Helpers;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.ItemStriker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Transmission.API.RPC;
using Transmission.API.RPC.Arguments;
using Transmission.API.RPC.Entity;

namespace Infrastructure.Verticals.DownloadClient.Transmission;

public sealed class TransmissionService : DownloadServiceBase
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
        FilenameEvaluator filenameEvaluator,
        Striker striker
    ) : base(logger, queueCleanerConfig, contentBlockerConfig, filenameEvaluator, striker)
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
    public override async Task<RemoveResult> ShouldRemoveFromArrQueueAsync(string hash)
    {
        RemoveResult result = new();
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

        // remove if all files are unwanted or download is stuck
        result.ShouldRemove = shouldRemove || IsItemStuckAndShouldRemove(torrent);
        
        return result;
    }

    /// <inheritdoc/>
    public override async Task<bool> BlockUnwantedFilesAsync(
        string hash,
        BlocklistType blocklistType,
        ConcurrentBag<string> patterns,
        ConcurrentBag<Regex> regexes
    )
    {
        TorrentInfo? torrent = await GetTorrentAsync(hash);

        if (torrent?.FileStats is null || torrent.Files is null)
        {
            return false;
        }
        
        if (_contentBlockerConfig.IgnorePrivate && (torrent.IsPrivate ?? false))
        {
            // ignore private trackers
            _logger.LogDebug("skip files check | download is private | {name}", torrent.Name);
            return false;
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
            return false;
        }

        if (totalUnwantedFiles == totalFiles)
        {
            // Skip marking files as unwanted. The download will be removed completely.
            return true;
        }
        
        _logger.LogDebug("changing priorities | torrent {hash}", hash);
        
        await _client.TorrentSetAsync(new TorrentSettings
        {
            Ids = [ torrent.Id ],
            FilesUnwanted = unwantedFiles.ToArray(),
        });

        return false;
    }

    public override async Task Delete(string hash)
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
    
    private bool IsItemStuckAndShouldRemove(TorrentInfo torrent)
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

        return StrikeAndCheckLimit(torrent.HashString!, torrent.Name!);
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
                TorrentFields.IS_PRIVATE
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