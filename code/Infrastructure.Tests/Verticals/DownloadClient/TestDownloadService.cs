using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.QueueCleaner;
using Domain.Enums;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.Files;
using Infrastructure.Verticals.ItemStriker;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Tests.Verticals.DownloadClient;

public class TestDownloadService : DownloadService
{
    public TestDownloadService(
        ILogger<DownloadService> logger,
        IOptions<QueueCleanerConfig> queueCleanerConfig,
        IOptions<ContentBlockerConfig> contentBlockerConfig,
        IOptions<DownloadCleanerConfig> downloadCleanerConfig,
        IMemoryCache cache,
        IFilenameEvaluator filenameEvaluator,
        IStriker striker,
        INotificationPublisher notifier,
        IDryRunInterceptor dryRunInterceptor,
        IHardLinkFileService hardLinkFileService
    ) : base(
        logger, queueCleanerConfig, contentBlockerConfig, downloadCleanerConfig, cache,
        filenameEvaluator, striker, notifier, dryRunInterceptor, hardLinkFileService
    )
    {
    }

    public override void Dispose() { }
    public override Task LoginAsync() => Task.CompletedTask;
    public override Task<DownloadCheckResult> ShouldRemoveFromArrQueueAsync(string hash, IReadOnlyList<string> ignoredDownloads) => Task.FromResult(new DownloadCheckResult());
    public override Task<BlockFilesResult> BlockUnwantedFilesAsync(string hash, BlocklistType blocklistType,
        ConcurrentBag<string> patterns, ConcurrentBag<Regex> regexes, IReadOnlyList<string> ignoredDownloads) => Task.FromResult(new BlockFilesResult());
    public override Task DeleteDownload(string hash) => Task.CompletedTask;
    public override Task CreateCategoryAsync(string name) => Task.CompletedTask;
    public override Task<List<object>?> GetSeedingDownloads() => Task.FromResult<List<object>?>(null);
    public override List<object>? FilterDownloadsToBeCleanedAsync(List<object>? downloads, List<CleanCategory> categories) => null;
    public override List<object>? FilterDownloadsToChangeCategoryAsync(List<object>? downloads, List<string> categories) => null;
    public override Task CleanDownloadsAsync(List<object>? downloads, List<CleanCategory> categoriesToClean, HashSet<string> excludedHashes, IReadOnlyList<string> ignoredDownloads) => Task.CompletedTask;
    public override Task ChangeCategoryForNoHardLinksAsync(List<object>? downloads, HashSet<string> excludedHashes, IReadOnlyList<string> ignoredDownloads) => Task.CompletedTask;
    // Expose protected methods for testing
    public new void ResetStalledStrikesOnProgress(string hash, long downloaded) => base.ResetStalledStrikesOnProgress(hash, downloaded);
    public new SeedingCheckResult ShouldCleanDownload(double ratio, TimeSpan seedingTime, CleanCategory category) => base.ShouldCleanDownload(ratio, seedingTime, category);
}