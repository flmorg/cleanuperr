using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.QueueCleaner;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.Files;
using Infrastructure.Verticals.ItemStriker;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.DownloadClient;

public class DummyDownloadService : DownloadService
{
    public DummyDownloadService(
        ILogger<DownloadService> logger,
        IOptions<QueueCleanerConfig> queueCleanerConfig,
        IOptions<ContentBlockerConfig> contentBlockerConfig,
        IOptions<DownloadCleanerConfig> downloadCleanerConfig,
        IMemoryCache cache,
        IFilenameEvaluator filenameEvaluator,
        IStriker striker,
        NotificationPublisher notifier,
        IDryRunInterceptor dryRunInterceptor,
        IHardlinkFileService hardlinkFileService
    ) : base(
        logger, queueCleanerConfig, contentBlockerConfig, downloadCleanerConfig,
        cache, filenameEvaluator, striker, notifier, dryRunInterceptor, hardlinkFileService
    )
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

    public override Task<List<object>?> GetDownloadsToBeCleaned(List<CleanCategory> categories)
    {
        throw new NotImplementedException();
    }

    public override Task<List<object>?> GetDownloadsToChangeCategory(List<string> categories)
    {
        throw new NotImplementedException();
    }

    public override Task CleanDownloads(List<object> downloads, List<CleanCategory> categoriesToClean, HashSet<string> excludedHashes)
    {
        throw new NotImplementedException();
    }

    public override Task ChangeCategoryForNoHardlinksAsync(List<object> downloads, HashSet<string> excludedHashes)
    {
        throw new NotImplementedException();
    }

    public override Task DeleteDownload(string hash)
    {
        throw new NotImplementedException();
    }
}