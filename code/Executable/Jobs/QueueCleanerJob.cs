using Infrastructure.Verticals.QueueCleaner;
using Quartz;

namespace Executable.Jobs;

[DisallowConcurrentExecution]
public sealed class QueueCleanerJob : IJob
{
    private ILogger<QueueCleanerJob> _logger;
    private QueueCleanerHandler _handler;

    public QueueCleanerJob(ILogger<QueueCleanerJob> logger, QueueCleanerHandler handler)
    {
        _logger = logger;
        _handler = handler;
    }
    
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await _handler.HandleAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(QueueCleanerJob)} failed");
        }
    }
}