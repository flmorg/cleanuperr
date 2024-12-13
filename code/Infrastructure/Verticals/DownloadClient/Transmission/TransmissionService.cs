using Common.Configuration.DownloadClient;
using Infrastructure.Verticals.ContentBlocker;
using Microsoft.Extensions.Caching.Memory;
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
        ILogger<TransmissionService> logger,
        IOptions<TransmissionConfig> config,
        FilenameEvaluator filenameEvaluator,
        IMemoryCache cache
    ) : base(logger, filenameEvaluator, cache)
    {
        _config = config.Value;
        _config.Validate();
        _client = new(
            new Uri(_config.Url, "/transmission/rpc").ToString(),
            login: _config.Username,
            password: _config.Password
        );
    }

    public override async Task LoginAsync()
    {
        await _client.GetSessionInformationAsync();
    }

    public override async Task<bool> ShouldRemoveFromArrQueueAsync(string hash, ushort maxStrikes)
    {
        TorrentInfo? torrent = await GetTorrentAsync(hash);

        if (torrent is null)
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
            return false;
        }
        
        bool shouldRemove = torrent.FileStats?.Length > 0;

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

        // remove if all files are unwanted
        return shouldRemove || IsItemStuckAndShouldRemove(torrent, maxStrikes);
    }

    public override async Task BlockUnwantedFilesAsync(string hash)
    {
        TorrentInfo? torrent = await GetTorrentAsync(hash);

        if (torrent?.FileStats is null || torrent.Files is null)
        {
            return;
        }

        List<long> unwantedFiles = [];
        
        for (int i = 0; i < torrent.Files.Length; i++)
        {
            if (torrent.FileStats?[i].Wanted == null)
            {
                continue;
            }
            
            if (!torrent.FileStats[i].Wanted.Value || _filenameEvaluator.IsValid(torrent.Files[i].Name))
            {
                continue;
            }
            
            _logger.LogInformation("unwanted file found | {file}", torrent.Files[i].Name);
            unwantedFiles.Add(i);
        }

        if (unwantedFiles.Count is 0)
        {
            return;
        }
        
        _logger.LogDebug("changing priorities | torrent {hash}", hash);
        
        await _client.TorrentSetAsync(new TorrentSettings
        {
            Ids = [ torrent.Id ],
            FilesUnwanted = unwantedFiles.ToArray(),
        });
    }

    public override void Dispose()
    {
    }
    
    private bool IsItemStuckAndShouldRemove(TorrentInfo torrent, ushort maxStrikes)
    {
        if (torrent.Status is not 4)
        {
            // not in downloading state
            return false;
        }

        if (torrent.Eta > 0)
        {
            return false;
        }

        return StrikeAndCheckLimit(torrent.HashString, torrent.Name, maxStrikes);
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
                TorrentFields.STATUS
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