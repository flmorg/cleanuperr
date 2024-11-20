using Infrastructure.Verticals.Jobs;
using Quartz;

namespace Executable.Jobs;

[DisallowConcurrentExecution]
public sealed class GenericJob<T> : IJob
    where T : GenericHandler
{
    private readonly ILogger<GenericJob<T>> _logger;
    private readonly T _handler;


    public GenericJob(ILogger<GenericJob<T>> logger, T handler)
    {
        _logger = logger;
        _handler = handler;
    }
    
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await _handler.ExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{name} failed", typeof(T).Name);
        }
    }
}