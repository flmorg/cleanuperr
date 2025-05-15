using Common.Configuration;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.General;
using Common.Configuration.QueueCleaner;
using Common.Helpers;
using Executable.Jobs;
using Infrastructure.Configuration;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.DownloadCleaner;
using Infrastructure.Verticals.Jobs;
using Infrastructure.Verticals.QueueCleaner;
using Quartz;
using Quartz.Spi;

namespace Executable.DependencyInjection;

public static class QuartzDI
{
    public static IServiceCollection AddQuartzServices(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddQuartz(q =>
            {
                // We'll configure triggers from a dedicated JSON file
                var triggerConfigTask = services
                    .BuildServiceProvider()
                    .GetRequiredService<IConfigurationManager>()
                    .GetConfigurationAsync<TriggersConfig>("triggers.json");
                
                triggerConfigTask.Wait();
                TriggersConfig? config = triggerConfigTask.Result;

                if (config is null)
                {
                    // Create a default if it doesn't exist
                    config = new TriggersConfig
                    {
                        ContentBlocker = "0 */30 * ? * *", // Every 30 minutes
                        QueueCleaner = "0 */15 * ? * *",   // Every 15 minutes
                        DownloadCleaner = "0 */20 * ? * *" // Every 20 minutes
                    };
                }

                q.AddJobs(services.BuildServiceProvider(), config);
            })
            .AddQuartzHostedService(opt =>
            {
                opt.WaitForJobsToComplete = true;
            });

    private static void AddJobs(
        this IServiceCollectionQuartzConfigurator q,
        IServiceProvider serviceProvider,
        TriggersConfig triggersConfig
    )
    {
        var configManager = serviceProvider.GetRequiredService<IConfigurationManager>();
        
        // Get configurations from JSON files
        var contentBlockerConfigTask = configManager.GetContentBlockerConfigAsync();
        contentBlockerConfigTask.Wait();
        ContentBlockerConfig? contentBlockerConfig = contentBlockerConfigTask.Result;
        
        q.AddJob<ContentBlocker>(contentBlockerConfig, triggersConfig.ContentBlocker);
        
        var queueCleanerConfigTask = configManager.GetQueueCleanerConfigAsync();
        queueCleanerConfigTask.Wait();
        QueueCleanerConfig? queueCleanerConfig = queueCleanerConfigTask.Result;

        if (contentBlockerConfig?.Enabled is true && queueCleanerConfig is { Enabled: true, RunSequentially: true })
        {
            q.AddJob<QueueCleaner>(queueCleanerConfig, string.Empty);
            q.AddJobListener(new JobChainingListener(nameof(ContentBlocker), nameof(QueueCleaner)));
        }
        else
        {
            q.AddJob<QueueCleaner>(queueCleanerConfig, triggersConfig.QueueCleaner);
        }
        
        var downloadCleanerConfigTask = configManager.GetDownloadCleanerConfigAsync();
        downloadCleanerConfigTask.Wait();
        DownloadCleanerConfig? downloadCleanerConfig = downloadCleanerConfigTask.Result;

        q.AddJob<DownloadCleaner>(downloadCleanerConfig, triggersConfig.DownloadCleaner);
    }
    
    private static void AddJob<T>(
        this IServiceCollectionQuartzConfigurator q,
        IJobConfig? config,
        string trigger
    ) where T: GenericHandler
    {
        string typeName = typeof(T).Name;
        
        if (config is null)
        {
            throw new NullReferenceException($"{typeName} configuration is null");
        }

        if (!config.Enabled)
        {
            return;
        }

        bool hasTrigger = trigger.Length > 0;

        q.AddJob<GenericJob<T>>(opts =>
        {
            opts.WithIdentity(typeName);

            if (!hasTrigger)
            {
                // jobs with no triggers need to be stored durably
                opts.StoreDurably();
            }
        });

        // skip empty triggers
        if (!hasTrigger)
        {
            return;
        }
        
        IOperableTrigger triggerObj = (IOperableTrigger)TriggerBuilder.Create()
            .WithIdentity("ExampleTrigger")
            .StartNow()
            .WithCronSchedule(trigger)
            .Build();

        IReadOnlyList<DateTimeOffset> nextFireTimes = TriggerUtils.ComputeFireTimes(triggerObj, null, 2);
        TimeSpan triggerValue = nextFireTimes[1] - nextFireTimes[0];
        
        if (triggerValue > Constants.TriggerMaxLimit)
        {
            throw new Exception($"{trigger} should have a fire time of maximum {Constants.TriggerMaxLimit.TotalHours} hours");
        }

        if (triggerValue > StaticConfiguration.TriggerValue)
        {
            StaticConfiguration.TriggerValue = triggerValue;
        }
        
        q.AddTrigger(opts =>
        {
            opts.ForJob(typeName)
                .WithIdentity($"{typeName}-trigger")
                .WithCronSchedule(trigger, x =>x.WithMisfireHandlingInstructionDoNothing())
                .StartNow();
        });
        
        // Startup trigger
        q.AddTrigger(opts =>
        {
            opts.ForJob(typeName)
                .WithIdentity($"{typeName}-startup-trigger")
                .StartNow();
        });
    }
}