using Common.Configuration;
using Common.Configuration.DownloadClient;
using Domain.Models.Deluge.Response;
using Infrastructure.Verticals.ContentBlocker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.DownloadClient.Deluge;

public sealed class DelugeService : IDownloadService
{
    private readonly ILogger<DelugeService> _logger;
    private readonly DelugeClient _client;
    private readonly FilenameEvaluator _filenameEvaluator;
    
    public DelugeService(
        ILogger<DelugeService> logger,
        IOptions<DelugeConfig> config,
        IHttpClientFactory httpClientFactory,
        FilenameEvaluator filenameEvaluator
    )
    {
        _logger = logger;
        config.Value.Validate();
        _client = new (config, httpClientFactory);
        _filenameEvaluator = filenameEvaluator;
    }
    
    public async Task LoginAsync()
    {
        await _client.LoginAsync();
    }

    public async Task<bool> ShouldRemoveFromArrQueueAsync(string hash)
    {
        hash = hash.ToLowerInvariant();
        
        DelugeContents? contents = null;
        
        if (!await HasMinimalStatus(hash))
        {
            return false;
        }

        try
        {
            contents = await _client.GetTorrentFiles(hash);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "failed to find torrent {hash} in the download client", hash);
        }

        // if no files found, torrent might be stuck in Downloading metadata
        if (contents?.Contents?.Count is null or 0)
        {
            return false;
        }

        bool shouldRemove = true;
        
        ProcessFiles(contents.Contents, (_, file) =>
        {
            if (file.Priority > 0)
            {
                shouldRemove = false;
            }
        });

        return shouldRemove;
    }

    public async Task BlockUnwantedFilesAsync(string hash)
    {
        hash = hash.ToLowerInvariant();

        if (!await HasMinimalStatus(hash))
        {
            return;
        }
        
        DelugeContents? contents = null;

        try
        {
            contents = await _client.GetTorrentFiles(hash);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "failed to find torrent {hash} in the download client", hash);
        }

        if (contents is null)
        {
            return;
        }
        
        Dictionary<int, int> priorities = [];
        bool hasPriorityUpdates = false;

        ProcessFiles(contents.Contents, (name, file) =>
        {
            int priority = file.Priority;

            if (file.Priority is not 0 && !_filenameEvaluator.IsValid(name))
            {
                priority = 0;
                hasPriorityUpdates = true;
                _logger.LogInformation("unwanted file found | {file}", file.Path);
            }
            
            priorities.Add(file.Index, priority);
        });

        if (!hasPriorityUpdates)
        {
            return;
        }
        
        _logger.LogDebug("changing priorities | torrent {hash}", hash);

        List<int> sortedPriorities = priorities
            .OrderBy(x => x.Key)
            .Select(x => x.Value)
            .ToList();

        await _client.ChangeFilesPriority(hash, sortedPriorities);
    }

    private async Task<bool> HasMinimalStatus(string hash)
    {
        DelugeMinimalStatus? status = await _client.SendRequest<DelugeMinimalStatus?>(
            "web.get_torrent_status",
            hash,
            new[] { "hash" }
        );

        if (status?.Hash is null)
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
            return false;
        }

        return true;
    }
    
    private static void ProcessFiles(Dictionary<string, DelugeFileOrDirectory> contents, Action<string, DelugeFileOrDirectory> processFile)
    {
        foreach (var (name, data) in contents)
        {
            switch (data.Type)
            {
                case "file":
                    processFile(name, data);
                    break;
                case "dir" when data.Contents is not null:
                    // Recurse into subdirectories
                    ProcessFiles(data.Contents, processFile);
                    break;
            }
        }
    }

    public void Dispose()
    {
    }
}