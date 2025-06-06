using Common.Configuration;
using Infrastructure.Models;
using Infrastructure.Utilities;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Collections.Concurrent;
using Infrastructure.Services.Interfaces;
using Quartz.Impl.Matchers;

namespace Infrastructure.Services;

public class JobManagementService : IJobManagementService
{
    private readonly ILogger<JobManagementService> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ConcurrentDictionary<string, JobKey> _jobKeys = new();

    public JobManagementService(ILogger<JobManagementService> logger, ISchedulerFactory schedulerFactory)
    {
        _logger = logger;
        _schedulerFactory = schedulerFactory;
    }

    public async Task<bool> StartJob(JobType jobType, JobSchedule? schedule = null, string? directCronExpression = null)
    {
        string jobName = jobType.ToJobName();
        string? cronExpression = directCronExpression ?? schedule?.ToCronExpression();
        
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey(jobName);
            
            // Check if job exists
            if (!await scheduler.CheckExists(jobKey))
            {
                _logger.LogError("Job {name} does not exist", jobName);
                return false;
            }

            // Store the job key for later use
            _jobKeys.TryAdd(jobName, jobKey);

            // If cron expression is provided, update the trigger
            if (!string.IsNullOrEmpty(cronExpression))
            {
                var triggerKey = new TriggerKey($"{jobName}Trigger");
                var existingTrigger = await scheduler.GetTrigger(triggerKey);
                
                if (existingTrigger != null)
                {
                    var newTrigger = TriggerBuilder.Create()
                        .WithIdentity(triggerKey)
                        .ForJob(jobKey)
                        .WithCronSchedule(cronExpression)
                        .Build();
                    
                    await scheduler.RescheduleJob(triggerKey, newTrigger);
                }
                else
                {
                    var trigger = TriggerBuilder.Create()
                        .WithIdentity(triggerKey)
                        .ForJob(jobKey)
                        .WithCronSchedule(cronExpression)
                        .Build();
                    
                    await scheduler.ScheduleJob(trigger);
                }
            }
            else
            {
                // If no trigger exists, create a simple one-time trigger
                var triggers = await scheduler.GetTriggersOfJob(jobKey);
                if (!triggers.Any())
                {
                    var trigger = TriggerBuilder.Create()
                        .WithIdentity($"{jobName}Trigger")
                        .ForJob(jobKey)
                        .StartNow()
                        .Build();
                    
                    await scheduler.ScheduleJob(trigger);
                }
            }

            // Resume the job if it's paused
            await scheduler.ResumeJob(jobKey);
            _logger.LogInformation("Job {name} started successfully", jobName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting job {jobName}", jobName);
            return false;
        }
    }

    public async Task<bool> StopJob(JobType jobType)
    {
        string jobName = jobType.ToJobName();
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey(jobName);
            
            if (!await scheduler.CheckExists(jobKey))
            {
                _logger.LogError("Job {name} does not exist", jobName);
                return false;
            }

            // Unschedule all triggers for this job
            var triggers = await scheduler.GetTriggersOfJob(jobKey);
            foreach (var trigger in triggers)
            {
                await scheduler.UnscheduleJob(trigger.Key);
            }

            _logger.LogInformation("Job {name} stopped successfully", jobName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping job {jobName}", jobName);
            return false;
        }
    }

    public async Task<bool> PauseJob(JobType jobType)
    {
        string jobName = jobType.ToJobName();
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey(jobName);
            
            if (!await scheduler.CheckExists(jobKey))
            {
                _logger.LogError("Job {name} does not exist", jobName);
                return false;
            }

            await scheduler.PauseJob(jobKey);
            _logger.LogInformation("Job {name} paused successfully", jobName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing job {jobName}", jobName);
            return false;
        }
    }

    public async Task<bool> ResumeJob(JobType jobType)
    {
        string jobName = jobType.ToJobName();
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey(jobName);
            
            if (!await scheduler.CheckExists(jobKey))
            {
                _logger.LogError("Job {name} does not exist", jobName);
                return false;
            }

            await scheduler.ResumeJob(jobKey);
            _logger.LogInformation("Job {name} resumed successfully", jobName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming job {name}", jobName);
            return false;
        }
    }

    public async Task<IReadOnlyList<JobInfo>> GetAllJobs()
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var result = new List<JobInfo>();
            
            var jobGroups = await scheduler.GetJobGroupNames();
            foreach (var group in jobGroups)
            {
                var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(group));
                foreach (var jobKey in jobKeys)
                {
                    var jobDetail = await scheduler.GetJobDetail(jobKey);
                    var triggers = await scheduler.GetTriggersOfJob(jobKey);
                    var jobInfo = new JobInfo
                    {
                        Name = jobKey.Name,
                        JobType = jobDetail.JobType.Name,
                        Status = "Not Scheduled"
                    };
                    
                    if (triggers.Any())
                    {
                        var trigger = triggers.First();
                        var triggerState = await scheduler.GetTriggerState(trigger.Key);
                        
                        jobInfo.Status = triggerState switch
                        {
                            TriggerState.Normal => "Running",
                            TriggerState.Paused => "Paused",
                            TriggerState.Complete => "Complete",
                            TriggerState.Error => "Error",
                            TriggerState.Blocked => "Blocked",
                            TriggerState.None => "None",
                            _ => "Unknown"
                        };
                        
                        if (trigger is ICronTrigger cronTrigger)
                        {
                            jobInfo.Schedule = cronTrigger.CronExpressionString;
                        }
                        
                        jobInfo.NextRunTime = trigger.GetNextFireTimeUtc()?.UtcDateTime;
                        jobInfo.PreviousRunTime = trigger.GetPreviousFireTimeUtc()?.UtcDateTime;
                    }
                    
                    result.Add(jobInfo);
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all jobs");
            return new List<JobInfo>();
        }
    }

    public async Task<JobInfo> GetJob(JobType jobType)
    {
        string jobName = jobType.ToJobName();
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey(jobName);
            
            if (!await scheduler.CheckExists(jobKey))
            {
                _logger.LogError("Job {name} does not exist", jobName);
                return new JobInfo { Name = jobName, Status = "Not Found" };
            }

            var jobDetail = await scheduler.GetJobDetail(jobKey);
            var triggers = await scheduler.GetTriggersOfJob(jobKey);
            
            var jobInfo = new JobInfo
            {
                Name = jobName,
                JobType = jobDetail.JobType.Name,
                Status = "Not Scheduled"
            };
            
            if (triggers.Any())
            {
                var trigger = triggers.First();
                var state = await scheduler.GetTriggerState(trigger.Key);
                
                jobInfo.Status = state switch
                {
                    TriggerState.Normal => "Running",
                    TriggerState.Paused => "Paused",
                    TriggerState.Complete => "Complete",
                    TriggerState.Error => "Error",
                    TriggerState.Blocked => "Blocked",
                    TriggerState.None => "None",
                    _ => "Unknown"
                };
                
                if (trigger is ICronTrigger cronTrigger)
                {
                    jobInfo.Schedule = cronTrigger.CronExpressionString;
                }
                
                jobInfo.NextRunTime = trigger.GetNextFireTimeUtc()?.LocalDateTime;
                jobInfo.PreviousRunTime = trigger.GetPreviousFireTimeUtc()?.LocalDateTime;
            }
            
            return jobInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job {name}", jobName);
            return new JobInfo { Name = jobName, Status = "Error" };
        }
    }

    public async Task<bool> UpdateJobSchedule(JobType jobType, JobSchedule schedule)
    {
        if (schedule == null)
            throw new ArgumentNullException(nameof(schedule));
            
        string jobName = jobType.ToJobName();
        string cronExpression = schedule.ToCronExpression();
        
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey(jobName);
            
            if (!await scheduler.CheckExists(jobKey))
            {
                _logger.LogError("Job {name} does not exist", jobName);
                return false;
            }

            var triggerKey = new TriggerKey($"{jobName}Trigger");
            var existingTrigger = await scheduler.GetTrigger(triggerKey);
            
            var newTrigger = TriggerBuilder.Create()
                .WithIdentity(triggerKey)
                .ForJob(jobKey)
                .WithCronSchedule(cronExpression)
                .Build();
            
            if (existingTrigger != null)
            {
                await scheduler.RescheduleJob(triggerKey, newTrigger);
            }
            else
            {
                await scheduler.ScheduleJob(newTrigger);
            }
            
            _logger.LogInformation("Job {name} schedule updated successfully to {cronExpression}", jobName, cronExpression);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job {name} schedule", jobName);
            return false;
        }
    }
}
