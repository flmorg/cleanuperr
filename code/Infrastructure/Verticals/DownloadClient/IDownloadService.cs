using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.ContentBlocker;

namespace Infrastructure.Verticals.DownloadClient;

public interface IDownloadService : IDisposable
{
    public Task LoginAsync();

    /// <summary>
    /// Checks whether the download should be removed from the *arr queue.
    /// </summary>
    /// <param name="hash">The download hash.</param>
    public Task<RemoveResult> ShouldRemoveFromArrQueueAsync(string hash);

    /// <summary>
    /// Blocks unwanted files from being fully downloaded.
    /// </summary>
    /// <param name="hash">The torrent hash.</param>
    /// <param name="blocklistType">The <see cref="BlocklistType"/>.</param>
    /// <param name="patterns">The patterns to test the files against.</param>
    /// <param name="regexes">The regexes to test the files against.</param>
    /// <returns>True if all files have been blocked; otherwise false.</returns>
    public Task<bool> BlockUnwantedFilesAsync(
        string hash,
        BlocklistType blocklistType,
        ConcurrentBag<string> patterns,
        ConcurrentBag<Regex> regexes
    );

    /// <summary>
    /// Deletes a download item.
    /// </summary>
    public Task Delete(string hash);
}