using Common.Configuration.QueueCleaner;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.ItemStriker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.DownloadClient;

public sealed class DummyDownloadService : DownloadServiceBase
{
    public DummyDownloadService(ILogger<DownloadServiceBase> logger, IOptions<QueueCleanerConfig> queueCleanerConfig, FilenameEvaluator filenameEvaluator, Striker striker) : base(logger, queueCleanerConfig, filenameEvaluator, striker)
    {
    }

    public override void Dispose()
    {
    }

    public override Task LoginAsync()
    {
        return Task.CompletedTask;
    }

    public override Task<RemoveResult> ShouldRemoveFromArrQueueAsync(string hash)
    {
        throw new NotImplementedException();
    }

    public override Task BlockUnwantedFilesAsync(string hash)
    {
        throw new NotImplementedException();
    }
}