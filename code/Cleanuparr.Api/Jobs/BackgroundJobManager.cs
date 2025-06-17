using Cleanuparr.Application.Features.DownloadCleaner;
using Cleanuparr.Application.Features.QueueCleaner;
using Cleanuparr.Domain.Exceptions;
using Cleanuparr.Infrastructure.Features.Jobs;
using Cleanuparr.Persistence;
using Cleanuparr.Persistence.Models.Configuration;
using Cleanuparr.Persistence.Models.Configuration.DownloadCleaner;
using Cleanuparr.Persistence.Models.Configuration.QueueCleaner;
using Cleanuparr.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Quartz.Spi;

namespace Cleanuparr.Api.Jobs;

/// <summary>
/// Manages background jobs in the application.
/// This class is responsible for reading configurations and scheduling jobs.
/// </summary>
public class BackgroundJobManager : IHostedService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly DataContext _dataContext;
    private readonly ILogger<BackgroundJobManager> _logger;
    private IScheduler? _scheduler;

    public BackgroundJobManager(
        ISchedulerFactory schedulerFactory,
        DataContext dataContext,
        ILogger<BackgroundJobManager> logger
    )
    {
        _schedulerFactory = schedulerFactory;
        _dataContext = dataContext;
        _logger = logger;
    }

    /// <summary>
    /// Starts the background job manager.
    /// This method is called when the application starts.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting BackgroundJobManager");
            _scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        
            await InitializeJobsFromConfiguration(cancellationToken);
        
            _logger.LogInformation("BackgroundJobManager started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start BackgroundJobManager");
        }
    }

    /// <summary>
    /// Stops the background job manager.
    /// This method is called when the application stops.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping BackgroundJobManager");
        
        if (_scheduler != null)
        {
            // Don't shutdown the scheduler as it's managed by QuartzHostedService
            await _scheduler.Standby(cancellationToken);
        }
        
        _logger.LogInformation("BackgroundJobManager stopped");
    }
    
    /// <summary>
    /// Initializes jobs based on current configuration settings.
    /// </summary>
    private async Task InitializeJobsFromConfiguration(CancellationToken cancellationToken = default)
    {
        if (_scheduler == null)
        {
            throw new InvalidOperationException("Scheduler not initialized");
        }
        
        // Get configurations from db
        QueueCleanerConfig queueCleanerConfig = await _dataContext.QueueCleanerConfigs
            .AsNoTracking()
            .FirstAsync(cancellationToken);
        DownloadCleanerConfig downloadCleanerConfig = await _dataContext.DownloadCleanerConfigs
            .AsNoTracking()
            .FirstAsync(cancellationToken);
        
        await AddQueueCleanerJob(queueCleanerConfig, cancellationToken);
        await AddDownloadCleanerJob(downloadCleanerConfig, cancellationToken);
    }
    
    /// <summary>
    /// Adds the QueueCleaner job to the scheduler.
    /// </summary>
    public async Task AddQueueCleanerJob(
        QueueCleanerConfig config, 
        CancellationToken cancellationToken = default)
    {
        if (!config.Enabled)
        {
            return;
        }
        
        await AddJobWithTrigger<QueueCleaner>(
            config, 
            config.CronExpression, 
            cancellationToken);
    }
    
    /// <summary>
    /// Adds the DownloadCleaner job to the scheduler.
    /// </summary>
    public async Task AddDownloadCleanerJob(DownloadCleanerConfig config, CancellationToken cancellationToken = default)
    {
        if (!config.Enabled)
        {
            return;
        }
        
        await AddJobWithTrigger<DownloadCleaner>(
            config, 
            config.CronExpression, 
            cancellationToken);
    }
    
    /// <summary>
    /// Helper method to add a job with a cron trigger.
    /// </summary>
    private async Task AddJobWithTrigger<T>(
        IJobConfig config,
        string cronExpression,
        CancellationToken cancellationToken = default) 
        where T : GenericHandler
    {
        if (_scheduler == null)
        {
            throw new InvalidOperationException("Scheduler not initialized");
        }
        
        if (!config.Enabled)
        {
            return;
        }
        
        string typeName = typeof(T).Name;
        var jobKey = new JobKey(typeName);
        
        // Create job detail
        var jobDetail = JobBuilder.Create<GenericJob<T>>()
            .StoreDurably()
            .WithIdentity(jobKey)
            .Build();
        
        // Add job to scheduler
        await _scheduler.AddJob(jobDetail, true, cancellationToken);
        
        // Validate the cron expression
        if (!string.IsNullOrEmpty(cronExpression))
        {
            IOperableTrigger triggerObj = (IOperableTrigger)TriggerBuilder.Create()
                .WithIdentity("ValidationTrigger")
                .StartNow()
                .WithCronSchedule(cronExpression)
                .Build();

            IReadOnlyList<DateTimeOffset> nextFireTimes = TriggerUtils.ComputeFireTimes(triggerObj, null, 2);
            TimeSpan triggerValue = nextFireTimes[1] - nextFireTimes[0];
            
            if (triggerValue > Constants.TriggerMaxLimit)
            {
                throw new ValidationException($"{cronExpression} should have a fire time of maximum {Constants.TriggerMaxLimit.TotalHours} hours");
            }
            
            if (triggerValue < Constants.TriggerMinLimit)
            {
                throw new ValidationException($"{cronExpression} should have a fire time of minimum {Constants.TriggerMinLimit.TotalSeconds} seconds");
            }

            if (triggerValue > StaticConfiguration.TriggerValue)
            {
                StaticConfiguration.TriggerValue = triggerValue;
            }
        }
        
        // Create cron trigger
        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{typeName}-trigger")
            .ForJob(jobKey)
            .WithCronSchedule(cronExpression, x => x.WithMisfireHandlingInstructionDoNothing())
            .StartNow()
            .Build();
        
        // Create startup trigger to run immediately
        var startupTrigger = TriggerBuilder.Create()
            .WithIdentity($"{typeName}-startup-trigger")
            .ForJob(jobKey)
            .StartNow()
            .Build();
        
        // Schedule job with both triggers
        await _scheduler.ScheduleJob(trigger, cancellationToken);
        await _scheduler.ScheduleJob(startupTrigger, cancellationToken);
        
        _logger.LogInformation("Added job {name} with cron expression {CronExpression}", 
            typeName, cronExpression);
    }
    
    /// <summary>
    /// Helper method to add a job without a trigger (for chained jobs).
    /// </summary>
    private async Task AddJobWithoutTrigger<T>(CancellationToken cancellationToken = default) 
        where T : GenericHandler
    {
        if (_scheduler == null)
        {
            throw new InvalidOperationException("Scheduler not initialized");
        }
        
        string typeName = typeof(T).Name;
        var jobKey = new JobKey(typeName);
        
        // Create job detail that is durable (can exist without triggers)
        var jobDetail = JobBuilder.Create<GenericJob<T>>()
            .WithIdentity(jobKey)
            .StoreDurably()
            .Build();
        
        // Add job to scheduler
        await _scheduler.AddJob(jobDetail, true, cancellationToken);
        
        _logger.LogInformation("Added job {name} without trigger (will be chained)", typeName);
    }
}
