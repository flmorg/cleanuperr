using Common.Configuration;
using Infrastructure.Verticals.ContentBlocker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Transmission.API.RPC;
using Transmission.API.RPC.Arguments;
using Transmission.API.RPC.Entity;

namespace Infrastructure.Verticals.DownloadClient.Transmission;

public sealed class TransmissionService : IDownloadService
{
    private readonly ILogger<TransmissionService> _logger;
    private readonly TransmissionConfig _config;
    private readonly Client _client;
    private readonly FilenameEvaluator _filenameEvaluator;
    private TorrentInfo[]? _torrentsCache;

    public TransmissionService(
        ILogger<TransmissionService> logger,
        IOptions<TransmissionConfig> config,
        FilenameEvaluator filenameEvaluator
    )
    {
        _logger = logger;
        _config = config.Value;
        _client = new(
            new Uri(_config.Url, "/transmission/rpc").ToString(),
            login: _config.Username,
            password: _config.Password
        );
        _filenameEvaluator = filenameEvaluator;
    }

    public async Task LoginAsync()
    {
        await _client.GetSessionInformationAsync();
    }

    public async Task<bool> ShouldRemoveFromArrQueueAsync(string hash)
    {
        TorrentInfo? torrent = await GetTorrentAsync(hash);

        if (torrent is null)
        {
            return false;
        }

        foreach (TransmissionTorrentFileStats? stats in torrent.FileStats ?? [])
        {
            if (!stats.Wanted.HasValue)
            {
                // if any files stats are missing, do not remove
                return false;
            }
            
            if (stats.Wanted.HasValue && stats.Wanted.Value)
            {
                // if any files are wanted, do not remove
                return false;
            }
        }

        // remove if all files are unwanted
        return true;
    }

    public async Task BlockUnwantedFilesAsync(string hash)
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
    
    public void Dispose()
    {
    }

    private async Task<TorrentInfo?> GetTorrentAsync(string hash)
    {
        TorrentInfo? torrent = _torrentsCache?
            .FirstOrDefault(x => x.HashString.Equals(hash, StringComparison.InvariantCultureIgnoreCase));
        
        if (_torrentsCache is null || torrent is null)
        {
            string[] fields = [TorrentFields.FILES, TorrentFields.FILE_STATS, TorrentFields.HASH_STRING, TorrentFields.ID];
            
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