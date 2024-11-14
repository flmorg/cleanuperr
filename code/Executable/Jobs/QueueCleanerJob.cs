using Infrastructure.Verticals.QueueCleaner;
using Quartz;

namespace Executable.Jobs;

[DisallowConcurrentExecution]
public sealed class QueueCleanerJob : IJob
{
    private readonly ILogger<QueueCleanerJob> _logger;
    private readonly QueueCleaner _queueCleaner;

    public QueueCleanerJob(
        ILogger<QueueCleanerJob> logger,
        QueueCleaner queueCleaner
    )
    {
        _logger = logger;
        _queueCleaner = queueCleaner;
    }
    
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await _queueCleaner.ExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(QueueCleanerJob)} failed");
        }
    }
}