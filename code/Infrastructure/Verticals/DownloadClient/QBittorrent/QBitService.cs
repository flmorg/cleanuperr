using Common.Configuration.DownloadClient;
using Common.Configuration.QueueCleaner;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.ItemStriker;
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
        IOptions<QBitConfig> config,
        IOptions<QueueCleanerConfig> queueCleanerConfig,
        FilenameEvaluator filenameEvaluator,
        Striker striker
    ) : base(logger, queueCleanerConfig, filenameEvaluator, striker)
    {
        _config = config.Value;
        _config.Validate();
        _client = new(_config.Url);
    }

    public override async Task LoginAsync()
    {
        if (string.IsNullOrEmpty(_config.Username) && string.IsNullOrEmpty(_config.Password))
        {
            return;
        }
        
        await _client.LoginAsync(_config.Username, _config.Password);
    }

    public override async Task<bool> ShouldRemoveFromArrQueueAsync(string hash)
    {
        TorrentInfo? torrent = (await _client.GetTorrentListAsync(new TorrentListQuery { Hashes = [hash] }))
            .FirstOrDefault();

        if (torrent is null)
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
            return false;
        }

        // if all files were blocked by qBittorrent
        if (torrent is { CompletionOn: not null, Downloaded: null or 0 })
        {
            return true;
        }

        IReadOnlyList<TorrentContent>? files = await _client.GetTorrentContentsAsync(hash);

        // if all files are marked as skip
        if (files?.Count is > 0 && files.All(x => x.Priority is TorrentContentPriority.Skip))
        {
            return true;
        }

        return IsItemStuckAndShouldRemove(torrent);
    }

    public override async Task BlockUnwantedFilesAsync(string hash)
    {
        IReadOnlyList<TorrentContent>? files = await _client.GetTorrentContentsAsync(hash);

        if (files is null)
        {
            return;
        }
        
        foreach (TorrentContent file in files)
        {
            if (!file.Index.HasValue)
            {
                continue;
            }

            if (file.Priority is TorrentContentPriority.Skip || _filenameEvaluator.IsValid(file.Name))
            {
                continue;
            }
            
            _logger.LogInformation("unwanted file found | {file}", file.Name);
            await _client.SetFilePriorityAsync(hash, file.Index.Value, TorrentContentPriority.Skip);
        }
    }

    public override void Dispose()
    {
        _client.Dispose();
    }
    
    private bool IsItemStuckAndShouldRemove(TorrentInfo torrent)
    {
        if (torrent.State is not TorrentState.StalledDownload and not TorrentState.FetchingMetadata
            and not TorrentState.ForcedFetchingMetadata)
        {
            // ignore other states
            return false;
        }

        return StrikeAndCheckLimit(torrent.Hash, torrent.Name);
    }
}