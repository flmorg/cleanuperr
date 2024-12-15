using Common.Configuration;
using Common.Configuration.DownloadClient;
using Infrastructure.Verticals.ContentBlocker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QBittorrent.Client;

namespace Infrastructure.Verticals.DownloadClient.QBittorrent;

public sealed class QBitService : IDownloadService
{
    private readonly ILogger<QBitService> _logger;
    private readonly QBitConfig _config;
    private readonly QBittorrentClient _client;
    private readonly FilenameEvaluator _filenameEvaluator;

    public QBitService(
        ILogger<QBitService> logger,
        IOptions<QBitConfig> config,
        FilenameEvaluator filenameEvaluator
    )
    {
        _logger = logger;
        _config = config.Value;
        _config.Validate();
        _client = new(_config.Url);
        _filenameEvaluator = filenameEvaluator;
    }

    public async Task LoginAsync()
    {
        if (string.IsNullOrEmpty(_config.Username) && string.IsNullOrEmpty(_config.Password))
        {
            return;
        }
        
        await _client.LoginAsync(_config.Username, _config.Password);
    }

    public async Task<bool> ShouldRemoveFromArrQueueAsync(string hash)
    {
        TorrentInfo? torrent = (await _client.GetTorrentListAsync(new TorrentListQuery { Hashes = [hash] }))
            .FirstOrDefault();

        if (torrent is null)
        {
            return false;
        }

        // if all files were blocked by qBittorrent
        if (torrent is { CompletionOn: not null, Downloaded: null or 0 })
        {
            return true;
        }

        IReadOnlyList<TorrentContent>? files = await _client.GetTorrentContentsAsync(hash);

        // if no files found, torrent might be stuck in Downloading metadata
        if (files?.Count is null or 0)
        {
            return false;
        }

        // if all files are marked as skip
        return files.All(x => x.Priority is TorrentContentPriority.Skip);
    }

    public async Task BlockUnwantedFilesAsync(string hash)
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

    public void Dispose()
    {
        _client.Dispose();
    }
}