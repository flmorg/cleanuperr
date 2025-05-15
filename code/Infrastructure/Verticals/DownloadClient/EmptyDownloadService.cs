using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.DownloadCleaner;
using Domain.Enums;

namespace Infrastructure.Verticals.DownloadClient;

/// <summary>
/// Empty implementation of IDownloadService that performs no operations
/// This is used when no download client is configured or available
/// </summary>
public sealed class EmptyDownloadService : IDownloadService
{
    private readonly ILogger<EmptyDownloadService> _logger;

    public EmptyDownloadService(ILogger<EmptyDownloadService> logger)
    {
        _logger = logger;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    public Task LoginAsync()
    {
        _logger.LogDebug("EmptyDownloadService: Login called (no-op)");
        return Task.CompletedTask;
    }

    public Task<DownloadCheckResult> ShouldRemoveFromArrQueueAsync(string hash, IReadOnlyList<string> ignoredDownloads)
    {
        _logger.LogDebug("EmptyDownloadService: ShouldRemoveFromArrQueueAsync called (no-op)");
        return Task.FromResult(new DownloadCheckResult());
    }

    public Task<BlockFilesResult> BlockUnwantedFilesAsync(string hash, BlocklistType blocklistType, ConcurrentBag<string> patterns, ConcurrentBag<Regex> regexes, IReadOnlyList<string> ignoredDownloads)
    {
        _logger.LogDebug("EmptyDownloadService: BlockUnwantedFilesAsync called (no-op)");
        return Task.FromResult(new BlockFilesResult());
    }

    public Task DeleteDownload(string hash)
    {
        _logger.LogDebug("EmptyDownloadService: DeleteDownload called (no-op)");
        return Task.CompletedTask;
    }

    public Task<List<object>?> GetSeedingDownloads()
    {
        _logger.LogDebug("EmptyDownloadService: GetSeedingDownloads called (no-op)");
        return Task.FromResult<List<object>?>(new List<object>());
    }

    public List<object>? FilterDownloadsToBeCleanedAsync(List<object>? downloads, List<CleanCategory> categories)
    {
        _logger.LogDebug("EmptyDownloadService: FilterDownloadsToBeCleanedAsync called (no-op)");
        return new List<object>();
    }

    public List<object>? FilterDownloadsToChangeCategoryAsync(List<object>? downloads, List<string> categories)
    {
        _logger.LogDebug("EmptyDownloadService: FilterDownloadsToChangeCategoryAsync called (no-op)");
        return new List<object>();
    }

    public Task CleanDownloadsAsync(List<object>? downloads, List<CleanCategory> categoriesToClean, HashSet<string> excludedHashes, IReadOnlyList<string> ignoredDownloads)
    {
        _logger.LogDebug("EmptyDownloadService: CleanDownloadsAsync called (no-op)");
        return Task.CompletedTask;
    }

    public Task ChangeCategoryForNoHardLinksAsync(List<object>? downloads, HashSet<string> excludedHashes, IReadOnlyList<string> ignoredDownloads)
    {
        _logger.LogDebug("EmptyDownloadService: ChangeCategoryForNoHardLinksAsync called (no-op)");
        return Task.CompletedTask;
    }

    public Task CreateCategoryAsync(string name)
    {
        _logger.LogDebug("EmptyDownloadService: CreateCategoryAsync called (no-op)");
        return Task.CompletedTask;
    }
}
