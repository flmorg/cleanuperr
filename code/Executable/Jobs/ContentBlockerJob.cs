using Infrastructure.Verticals.ContentBlocker;
using Quartz;

namespace Executable.Jobs;

[DisallowConcurrentExecution]
public sealed class ContentBlockerJob : IJob
{
    private readonly ILogger<QueueCleanerJob> _logger;
    private readonly ContentBlocker _contentBlocker;

    public ContentBlockerJob(
        ILogger<QueueCleanerJob> logger,
        ContentBlocker contentBlocker
    )
    {
        _logger = logger;
        _contentBlocker = contentBlocker;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await _contentBlocker.ExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(ContentBlockerJob)} failed");
        }
    }
}