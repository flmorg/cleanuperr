﻿using Quartz;

namespace Infrastructure.Verticals.Jobs;

public class JobChainingListener : IJobListener
{
    private readonly string _nextJobName;

    public JobChainingListener(string nextJobName)
    {
        _nextJobName = nextJobName;
    }

    public string Name => nameof(JobChainingListener);

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_nextJobName))
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