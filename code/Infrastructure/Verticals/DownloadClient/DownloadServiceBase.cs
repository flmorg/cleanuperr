using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.ContentBlocker;
using Common.Configuration.QueueCleaner;
using Domain.Enums;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.ItemStriker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.DownloadClient;

public abstract class DownloadServiceBase : IDownloadService
{
    protected readonly ILogger<DownloadServiceBase> _logger;
    protected readonly QueueCleanerConfig _queueCleanerConfig;
    protected readonly ContentBlockerConfig _contentBlockerConfig;
    protected readonly FilenameEvaluator _filenameEvaluator;
    protected readonly Striker _striker;
    
    protected DownloadServiceBase(
        ILogger<DownloadServiceBase> logger,
        IOptions<QueueCleanerConfig> queueCleanerConfig,
        IOptions<ContentBlockerConfig> contentBlockerConfig,
        FilenameEvaluator filenameEvaluator,
        Striker striker
    )
    {
        _logger = logger;
        _queueCleanerConfig = queueCleanerConfig.Value;
        _contentBlockerConfig = contentBlockerConfig.Value;
        _filenameEvaluator = filenameEvaluator;
        _striker = striker;
    }

    public abstract void Dispose();

    public abstract Task LoginAsync();

    public abstract Task<RemoveResult> ShouldRemoveFromArrQueueAsync(string hash);

    /// <inheritdoc/>
    public abstract Task<bool> BlockUnwantedFilesAsync(
        string hash,
        BlocklistType blocklistType,
        ConcurrentBag<string> patterns,
        ConcurrentBag<Regex> regexes
    );

    /// <inheritdoc/>
    public abstract Task Delete(string hash);

    /// <summary>
    /// Strikes an item and checks if the limit has been reached.
    /// </summary>
    /// <param name="hash">The torrent hash.</param>
    /// <param name="itemName">The name or title of the item.</param>
    /// <returns>True if the limit has been reached; otherwise, false.</returns>
    protected bool StrikeAndCheckLimit(string hash, string itemName)
    {
        return _striker.StrikeAndCheckLimit(hash, itemName, _queueCleanerConfig.StalledMaxStrikes, StrikeType.Stalled);
    }
}