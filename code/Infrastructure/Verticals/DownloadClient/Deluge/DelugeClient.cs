using Common.Configuration;
using Domain.Deluge.Response;
using Infrastructure.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.DownloadClient.Deluge;

public sealed class DelugeClient : BaseDelugeClient, IDownloadClient
{
    private readonly ILogger<DelugeClient> _logger;
    private readonly FilenameEvaluator _filenameEvaluator;
    
    public DelugeClient(
        ILogger<DelugeClient> logger,
        IOptions<DelugeConfig> config,
        IHttpClientFactory httpClientFactory,
        FilenameEvaluator filenameEvaluator
    ) : base(config, httpClientFactory)
    {
        _logger = logger;
        _filenameEvaluator = filenameEvaluator;
    }
    
    public new async Task LoginAsync()
    {
        await base.LoginAsync();
    }

    public async Task<bool> ShouldRemoveFromArrQueue(string hash)
    {
        hash = hash.ToLowerInvariant();
        
        DelugeContents? contents = null;

        // TODO {"method":"web.get_torrent_status","params":["09b09e4cc3b2ab8c27f7114c939b4f536ba0e5e6",[ "hash" ]],"id":31}
        try
        {
            contents = await GetTorrentFiles(hash);
        }
        catch
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
        }

        if (contents is null)
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

    public async Task BlockUnwantedFiles(string hash)
    {
        await _filenameEvaluator.LoadPatterns();
        hash = hash.ToLowerInvariant();
        DelugeContents? contents = null;

        // TODO {"method":"web.get_torrent_status","params":["09b09e4cc3b2ab8c27f7114c939b4f536ba0e5e6",[ "hash" ]],"id":31}
        try
        {
            contents = await GetTorrentFiles(hash);
        }
        catch
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
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
        
        _logger.LogDebug(
            "changing priorities | torrent {hash} | priorities {priorities}",
            hash, string.Join(',', priorities)
        );

        List<int> sortedPriorities = priorities
            .OrderBy(x => x.Key)
            .Select(x => x.Value)
            .ToList();

        await ChangeFilesPriority(hash, sortedPriorities);
    }
    
    private void ProcessFiles(Dictionary<string, DelugeFileOrDirectory> contents, Action<string, DelugeFileOrDirectory> processFile)
    {
        foreach (var item in contents)
        {
            var name = item.Key; // File or directory name
            var data = item.Value;

            if (data.Type == "file")
            {
                processFile(name, data);
            }
            else if (data.Type == "dir" && data.Contents is not null)
            {
                // Recurse into subdirectories
                ProcessFiles(data.Contents, processFile);
            }
        }
    }

    public void Dispose()
    {
    }
}