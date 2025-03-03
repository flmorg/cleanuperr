using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.QueueCleaner;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.DownloadClient;
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
        IDryRunInterceptor dryRunInterceptor
    ) : base(
        logger, queueCleanerConfig, contentBlockerConfig, downloadCleanerConfig, cache,
        filenameEvaluator, striker, notifier, dryRunInterceptor
    )
    {
    }

    public override void Dispose() { }
    public override Task LoginAsync() => Task.CompletedTask;
    public override Task<StalledResult> ShouldRemoveFromArrQueueAsync(string hash, IReadOnlyList<string> ignoredDownloads) => Task.FromResult(new StalledResult());
    public override Task<BlockFilesResult> BlockUnwantedFilesAsync(string hash, BlocklistType blocklistType,
        ConcurrentBag<string> patterns, ConcurrentBag<Regex> regexes, IReadOnlyList<string> ignoredDownloads) => Task.FromResult(new BlockFilesResult());
    public override Task DeleteDownload(string hash) => Task.CompletedTask;
    public override Task<List<object>?> GetAllDownloadsToBeCleaned(List<Category> categories) => Task.FromResult<List<object>?>(null);
    public override Task CleanDownloads(List<object> downloads, List<Category> categoriesToClean, HashSet<string> excludedHashes,
        IReadOnlyList<string> ignoredDownloads) => Task.CompletedTask;

    // Expose protected methods for testing
    public new void ResetStrikesOnProgress(string hash, long downloaded) => base.ResetStrikesOnProgress(hash, downloaded);
    public new Task<bool> StrikeAndCheckLimit(string hash, string itemName) => base.StrikeAndCheckLimit(hash, itemName);
    public new SeedingCheckResult ShouldCleanDownload(double ratio, TimeSpan seedingTime, Category category) => base.ShouldCleanDownload(ratio, seedingTime, category);
}