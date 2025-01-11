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
    protected readonly FilenameEvaluator _filenameEvaluator;
    protected readonly Striker _striker;
    
    protected DownloadServiceBase(
        ILogger<DownloadServiceBase> logger,
        IOptions<QueueCleanerConfig> queueCleanerConfig,
        FilenameEvaluator filenameEvaluator,
        Striker striker
    )
    {
        _logger = logger;
        _queueCleanerConfig = queueCleanerConfig.Value;
        _filenameEvaluator = filenameEvaluator;
        _striker = striker;
    }

    public abstract void Dispose();

    public abstract Task LoginAsync();

    public abstract Task<RemoveResult> ShouldRemoveFromArrQueueAsync(string hash);

    public abstract Task BlockUnwantedFilesAsync(string hash);

    protected bool StrikeAndCheckLimit(string hash, string itemName)
    {
        return _striker.StrikeAndCheckLimit(hash, itemName, _queueCleanerConfig.StalledMaxStrikes, StrikeType.Stalled);
    }
}