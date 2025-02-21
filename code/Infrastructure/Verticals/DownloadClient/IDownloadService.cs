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
    public Task<StalledResult> ShouldRemoveFromArrQueueAsync(string hash);

    /// <summary>
    /// Blocks unwanted files from being fully downloaded.
    /// </summary>
    /// <param name="hash">The torrent hash.</param>
    /// <param name="blocklistType">The <see cref="BlocklistType"/>.</param>
    /// <param name="patterns">The patterns to test the files against.</param>
    /// <param name="regexes">The regexes to test the files against.</param>
    /// <returns>True if all files have been blocked; otherwise false.</returns>
    public Task<BlockFilesResult> BlockUnwantedFilesAsync(
        string hash,
        BlocklistType blocklistType,
        ConcurrentBag<string> patterns,
        ConcurrentBag<Regex> regexes
    );

    /// <summary>
    /// Fetches all downloads that should be cleaned.
    /// </summary>
    /// <param name="categories">The categories by which to filter the downloads.</param>
    /// <returns>A list of downloads for the provided categories.</returns>
    Task<List<object>?> GetDownloadsToBeCleanedAsync(List<CleanCategory> categories);

    /// <summary>
    /// Fetches all downloads that should have their category changed.
    /// </summary>
    /// <param name="categories">The categories by which to filter the downloads.</param>
    /// <returns>A list of downloads for the provided categories.</returns>
    Task<List<object>?> GetDownloadsToChangeCategoryAsync(List<string> categories);
    
    /// <summary>
    /// Cleans the downloads.
    /// </summary>
    /// <param name="downloads">The downloads to clean.</param>
    /// <param name="categoriesToClean">The categories that should be cleaned.</param>
    /// <param name="excludedHashes">The hashes that should not be cleaned.</param>
    Task CleanDownloadsAsync(List<object> downloads, List<CleanCategory> categoriesToClean, HashSet<string> excludedHashes);

    /// <summary>
    /// Changes the category for downloads that have no hardlinks.
    /// </summary>
    /// <param name="downloads">The downloads to change.</param>
    /// <param name="excludedHashes"></param>
    Task ChangeCategoryForNoHardLinksAsync(List<object> downloads, HashSet<string> excludedHashes);
    
    /// <summary>
    /// Deletes a download item.
    /// </summary>
    public Task DeleteDownloadAsync(string hash);

    /// <summary>
    /// Creates a category.
    /// </summary>
    /// <param name="name">The category name.</param>
    public Task CreateCategoryAsync(string name);
}