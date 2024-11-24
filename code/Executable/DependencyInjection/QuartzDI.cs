using Common.Configuration;
using Common.Configuration.ContentBlocker;
using Common.Configuration.QueueCleaner;
using Executable.Jobs;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.Jobs;
using Infrastructure.Verticals.QueueCleaner;
using Quartz;

namespace Executable.DependencyInjection;

public static class QuartzDI
{
    public static IServiceCollection AddQuartzServices(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddQuartz(q =>
            {
                TriggersConfig? config = configuration
                    .GetRequiredSection(TriggersConfig.SectionName)
                    .Get<TriggersConfig>();

                if (config is null)
                {
                    throw new NullReferenceException("triggers configuration is null");
                }

                q.AddJobs(configuration, config);
            })
            .AddQuartzHostedService(opt =>
            {
                opt.WaitForJobsToComplete = true;
            });

    private static void AddJobs(
        this IServiceCollectionQuartzConfigurator q,
        IConfiguration configuration,
        TriggersConfig triggersConfig
    )
    {
        ContentBlockerConfig? contentBlockerConfig = configuration
            .GetRequiredSection(ContentBlockerConfig.SectionName)
            .Get<ContentBlockerConfig>();
        
        q.AddJob<ContentBlocker>(contentBlockerConfig, triggersConfig.ContentBlocker);
        
        QueueCleanerConfig? queueCleanerConfig = configuration
            .GetRequiredSection(QueueCleanerConfig.SectionName)
            .Get<QueueCleanerConfig>();

        if (contentBlockerConfig?.Enabled is true && queueCleanerConfig is { Enabled: true, RunSequentially: true })
        {
            q.AddJob<QueueCleaner>(queueCleanerConfig, string.Empty);
            q.AddJobListener(new JobChainingListener(nameof(QueueCleaner)));
        }
        else
        {
            q.AddJob<QueueCleaner>(queueCleanerConfig, triggersConfig.QueueCleaner);
        }
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