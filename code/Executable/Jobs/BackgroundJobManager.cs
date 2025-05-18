using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.QueueCleaner;
using Common.Helpers;
using Infrastructure.Configuration;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.DownloadCleaner;
using Infrastructure.Verticals.Jobs;
using Infrastructure.Verticals.QueueCleaner;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Spi;

namespace Executable.Jobs;

/// <summary>
/// Manages background jobs in the application.
/// This class is responsible for reading configurations and scheduling jobs.
/// </summary>
public class BackgroundJobManager : IHostedService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IConfigManager _configManager;
    private readonly ILogger<BackgroundJobManager> _logger;
    private IScheduler? _scheduler;

    public BackgroundJobManager(
        ISchedulerFactory schedulerFactory,
        IConfigManager configManager,
        IServiceProvider serviceProvider,
        ILogger<BackgroundJobManager> logger)
    {
        _schedulerFactory = schedulerFactory;
        _configManager = configManager;
        _logger = logger;
    }

    /// <summary>
    /// Starts the background job manager.
    /// This method is called when the application starts.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting BackgroundJobManager");
        _scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        
        await InitializeJobsFromConfiguration(cancellationToken);
        
        _logger.LogInformation("BackgroundJobManager started");
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
        
        // Get configurations from JSON files
        ContentBlockerConfig? contentBlockerConfig = await _configManager.GetConfigurationAsync<ContentBlockerConfig>();
        QueueCleanerConfig? queueCleanerConfig = await _configManager.GetConfigurationAsync<QueueCleanerConfig>();
        DownloadCleanerConfig? downloadCleanerConfig = await _configManager.GetConfigurationAsync<DownloadCleanerConfig>();
        
        // Add ContentBlocker job if enabled
        if (contentBlockerConfig?.Enabled == true)
        {
            await AddContentBlockerJob(contentBlockerConfig, cancellationToken);
        }
        
        // Add QueueCleaner job if enabled
        if (queueCleanerConfig?.Enabled == true)
        {
            // Check if we need to chain it after ContentBlocker
            bool shouldChainAfterContentBlocker = 
                contentBlockerConfig?.Enabled == true && 
                queueCleanerConfig.RunSequentially;
            
            await AddQueueCleanerJob(queueCleanerConfig, shouldChainAfterContentBlocker, cancellationToken);
        }
        
        // Add DownloadCleaner job if enabled
        if (downloadCleanerConfig?.Enabled == true)
        {
            await AddDownloadCleanerJob(downloadCleanerConfig, cancellationToken);
        }
    }
    
    /// <summary>
    /// Adds the ContentBlocker job to the scheduler.
    /// </summary>
    public async Task AddContentBlockerJob(ContentBlockerConfig config, CancellationToken cancellationToken = default)
    {
        if (!config.Enabled)
        {
            return;
        }
        
        await AddJobWithTrigger<ContentBlocker>(
            config, 
            config.CronExpression, 
            cancellationToken);
    }
    
    /// <summary>
    /// Adds the QueueCleaner job to the scheduler.
    /// </summary>
    public async Task AddQueueCleanerJob(
        QueueCleanerConfig config, 
        bool chainAfterContentBlocker = false,
        CancellationToken cancellationToken = default)
    {
        if (!config.Enabled)
        {
            return;
        }
        
        var jobKey = new JobKey(nameof(QueueCleaner));
        
        // If the job should be chained after ContentBlocker, add it without a cron trigger
        if (chainAfterContentBlocker)
        {
            await AddJobWithoutTrigger<QueueCleaner>(cancellationToken);
            
            // Add job listener to chain QueueCleaner after ContentBlocker
            if (_scheduler != null)
            {
                var chainListener = new JobChainingListener(nameof(ContentBlocker), nameof(QueueCleaner));
                _scheduler.ListenerManager.AddJobListener(chainListener, KeyMatcher<JobKey>.KeyEquals(new JobKey(nameof(ContentBlocker))));
            }
        }
        else
        {
            // Add job with normal cron trigger
            await AddJobWithTrigger<QueueCleaner>(
                config, 
                config.CronExpression, 
                cancellationToken);
        }
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
        Common.Configuration.IJobConfig config, 
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
                throw new Exception($"{cronExpression} should have a fire time of maximum {Constants.TriggerMaxLimit.TotalHours} hours");
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
        
        _logger.LogInformation("Added job {JobName} with cron expression {CronExpression}", 
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
        
        _logger.LogInformation("Added job {JobName} without trigger (will be chained)", typeName);
    }
}
