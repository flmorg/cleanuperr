using Quartz;

namespace Cleanuparr.Infrastructure.Features.Jobs;

public class JobChainingListener : IJobListener
{
    private readonly string _firstJobName;
    private readonly string _nextJobName;

    public JobChainingListener(string firstJobName, string nextJobName)
    {
        _firstJobName = firstJobName;
        _nextJobName = nextJobName;
    }

    public string Name => nameof(JobChainingListener);

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_nextJobName) || context.JobDetail.Key.Name == _nextJobName || context.JobDetail.Key.Name != _firstJobName)
        {
            return;
        }
        
        IScheduler scheduler = context.Scheduler;
        JobKey nextJobKey = new(_nextJobName);

        if (await scheduler.CheckExists(nextJobKey, cancellationToken))
        {
            await scheduler.TriggerJob(nextJobKey, cancellationToken);
        }
    }
}