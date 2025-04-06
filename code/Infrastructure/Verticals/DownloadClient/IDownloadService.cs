using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Infrastructure.Interceptors;

namespace Infrastructure.Verticals.DownloadClient;

public interface IDownloadService : IDisposable
{
    public Task LoginAsync();

    /// <summary>
    /// Checks whether the download should be removed from the *arr queue.
    /// </summary>
    /// <param name="hash">The download hash.</param>
    /// <param name="ignoredDownloads">Downloads to ignore from processing.</param>
    public Task<DownloadCheckResult> ShouldRemoveFromArrQueueAsync(string hash, IReadOnlyList<string> ignoredDownloads);

    /// <summary>
    /// Blocks unwanted files from being fully downloaded.
    /// </summary>
    /// <param name="hash">The torrent hash.</param>
    /// <param name="blocklistType">The <see cref="BlocklistType"/>.</param>
    /// <param name="patterns">The patterns to test the files against.</param>
    /// <param name="regexes">The regexes to test the files against.</param>
    /// <param name="ignoredDownloads">Downloads to ignore from processing.</param>
    /// <returns>True if all files have been blocked; otherwise false.</returns>
    public Task<BlockFilesResult> BlockUnwantedFilesAsync(string hash,
        BlocklistType blocklistType,
        ConcurrentBag<string> patterns,
        ConcurrentBag<Regex> regexes,
        IReadOnlyList<string> ignoredDownloads
    );

    /// <summary>
    /// Fetches all downloads.
    /// </summary>
    /// <param name="categories">The categories by which to filter the downloads.</param>
    /// <returns>A list of downloads for the provided categories.</returns>
    Task<List<object>?> GetAllDownloadsToBeCleaned(List<Category> categories);

    /// <summary>
    /// Cleans the downloads.
    /// </summary>
    /// <param name="downloads"></param>
    /// <param name="categoriesToClean">The categories that should be cleaned.</param>
    /// <param name="excludedHashes">The hashes that should not be cleaned.</param>
    /// <param name="ignoredDownloads">Downloads to ignore from processing.</param>
    public abstract Task CleanDownloads(List<object> downloads, List<Category> categoriesToClean, HashSet<string> excludedHashes,
        IReadOnlyList<string> ignoredDownloads);

    /// <summary>
    /// Deletes a download item.
    /// </summary>
    public Task DeleteDownload(string hash);
}