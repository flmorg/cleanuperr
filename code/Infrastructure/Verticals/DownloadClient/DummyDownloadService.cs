using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.QueueCleaner;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.ItemStriker;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.DownloadClient;

public sealed class DummyDownloadService : DownloadService
{
    public DummyDownloadService(ILogger<DownloadService> logger, IOptions<QueueCleanerConfig> queueCleanerConfig, IOptions<ContentBlockerConfig> contentBlockerConfig, IOptions<DownloadCleanerConfig> downloadCleanerConfig, IMemoryCache cache, IFilenameEvaluator filenameEvaluator, IStriker striker, NotificationPublisher notifier) : base(logger, queueCleanerConfig, contentBlockerConfig, downloadCleanerConfig, cache, filenameEvaluator, striker, notifier)
    {
    }

    public override void Dispose()
    {
    }

    public override Task LoginAsync()
    {
        return Task.CompletedTask;
    }

    public override Task<StalledResult> ShouldRemoveFromArrQueueAsync(string hash)
    {
        throw new NotImplementedException();
    }

    public override Task<BlockFilesResult> BlockUnwantedFilesAsync(string hash, BlocklistType blocklistType, ConcurrentBag<string> patterns, ConcurrentBag<Regex> regexes)
    {
        throw new NotImplementedException();
    }

    public override Task<List<object>?> GetAllDownloadsToBeCleaned(List<Category> categories)
    {
        throw new NotImplementedException();
    }

    public override Task CleanDownloads(List<object> downloads, List<Category> categoriesToClean, HashSet<string> excludedHashes)
    {
        throw new NotImplementedException();
    }

    public override Task DeleteDownload(string hash)
    {
        throw new NotImplementedException();
    }
}