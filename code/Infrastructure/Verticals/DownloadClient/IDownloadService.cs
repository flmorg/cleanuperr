using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.QueueCleaner;
using Data.Enums;
using Infrastructure.Interceptors;
using QBittorrent.Client;

namespace Infrastructure.Verticals.DownloadClient;

public interface IDownloadService : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for this download client
    /// </summary>
    /// <returns>The client ID</returns>
    Guid GetClientId();

    /// <summary>
    /// Initializes the download service with client-specific configuration
    /// </summary>
    /// <param name="downloadClientConfig">The client configuration</param>
    public void Initialize(Common.Configuration.DownloadClientConfig downloadClientConfig);
    
    public Task LoginAsync();

    /// <summary>
    /// Checks whether the download should be removed from the *arr queue.
    /// </summary>
    /// <param name="hash">The download hash.</param>
    /// <param name="ignoredDownloads">Downloads to ignore from processing.</param>
    public Task<DownloadCheckResult> ShouldRemoveFromArrQueueAsync(string hash,
        IReadOnlyList<string> ignoredDownloads);

    /// <summary>
    /// Fetches all seeding downloads.
    /// </summary>
    /// <returns>A list of downloads that are seeding.</returns>
    Task<List<object>?> GetSeedingDownloads();

    /// <summary>
    /// Filters downloads that should be cleaned.
    /// </summary>
    /// <param name="downloads">The downloads to filter.</param>
    /// <param name="categories">The categories by which to filter the downloads.</param>
    /// <returns>A list of downloads for the provided categories.</returns>
    List<object>? FilterDownloadsToBeCleanedAsync(List<object>? downloads, List<CleanCategory> categories);

    /// <summary>
    /// Filters downloads that should have their category changed.
    /// </summary>
    /// <param name="downloads">The downloads to filter.</param>
    /// <param name="categories">The categories by which to filter the downloads.</param>
    /// <returns>A list of downloads for the provided categories.</returns>
    List<object>? FilterDownloadsToChangeCategoryAsync(List<object>? downloads, List<string> categories);

    /// <summary>
    /// Cleans the downloads.
    /// </summary>
    /// <param name="downloads">The downloads to clean.</param>
    /// <param name="categoriesToClean">The categories that should be cleaned.</param>
    /// <param name="excludedHashes">The hashes that should not be cleaned.</param>
    /// <param name="ignoredDownloads">The downloads to ignore from processing.</param>
    Task CleanDownloadsAsync(List<object>? downloads, List<CleanCategory> categoriesToClean, HashSet<string> excludedHashes, IReadOnlyList<string> ignoredDownloads);

    /// <summary>
    /// Changes the category for downloads that have no hardlinks.
    /// </summary>
    /// <param name="downloads">The downloads to change.</param>
    /// <param name="excludedHashes">The hashes that should not be cleaned.</param>
    /// <param name="ignoredDownloads">The downloads to ignore from processing.</param>
    Task ChangeCategoryForNoHardLinksAsync(List<object>? downloads, HashSet<string> excludedHashes, IReadOnlyList<string> ignoredDownloads);
    
    /// <summary>
    /// Deletes a download item.
    /// </summary>
    public Task DeleteDownload(string hash);

    /// <summary>
    /// Creates a category.
    /// </summary>
    /// <param name="name">The category name.</param>
    public Task CreateCategoryAsync(string name);
}