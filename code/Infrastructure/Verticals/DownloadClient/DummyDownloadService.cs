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
using Infrastructure.Configuration;

namespace Infrastructure.Verticals.DownloadClient;

public class DummyDownloadService : DownloadService
{
    public DummyDownloadService(
        ILogger<DownloadService> logger,
        IConfigManager configManager,
        IMemoryCache cache,
        IFilenameEvaluator filenameEvaluator,
        IStriker striker,
        INotificationPublisher notifier,
        IDryRunInterceptor dryRunInterceptor,
        IHardLinkFileService hardLinkFileService
    ) : base(
        logger, configManager, cache, filenameEvaluator, striker, notifier, dryRunInterceptor, hardLinkFileService
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

    public override Task<DownloadCheckResult> ShouldRemoveFromArrQueueAsync(string hash, IReadOnlyList<string> ignoredDownloads)
    {
        throw new NotImplementedException();
    }

    public override Task<BlockFilesResult> BlockUnwantedFilesAsync(string hash, BlocklistType blocklistType, ConcurrentBag<string> patterns,
        ConcurrentBag<Regex> regexes, IReadOnlyList<string> ignoredDownloads)
    {
        throw new NotImplementedException();
    }

    public override Task<List<object>?> GetSeedingDownloads()
    {
        throw new NotImplementedException();
    }

    public override List<object>? FilterDownloadsToBeCleanedAsync(List<object>? downloads, List<CleanCategory> categories)
    {
        throw new NotImplementedException();
    }

    public override List<object>? FilterDownloadsToChangeCategoryAsync(List<object>? downloads, List<string> categories)
    {
        throw new NotImplementedException();
    }

    public override Task CleanDownloadsAsync(List<object>? downloads, List<CleanCategory> categoriesToClean, HashSet<string> excludedHashes, IReadOnlyList<string> ignoredDownloads)
    {
        throw new NotImplementedException();
    }

    public override Task ChangeCategoryForNoHardLinksAsync(List<object>? downloads, HashSet<string> excludedHashes, IReadOnlyList<string> ignoredDownloads)
    {
        throw new NotImplementedException();
    }

    public override Task CreateCategoryAsync(string name)
    {
        throw new NotImplementedException();
    }

    public override Task DeleteDownload(string hash)
    {
        throw new NotImplementedException();
    }
}