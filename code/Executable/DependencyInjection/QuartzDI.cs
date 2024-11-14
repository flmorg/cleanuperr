using Common.Configuration;
using Common.Configuration.ContentBlocker;
using Common.Configuration.QueueCleaner;
using Executable.Jobs;
using Infrastructure.Verticals.ContentBlocker;
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

                q.AddQueueCleanerJob(configuration, config.QueueCleaner);
                q.AddContentBlockerJob(configuration, config.ContentBlocker);
            })
            .AddQuartzHostedService(opt =>
            {
                opt.WaitForJobsToComplete = true;
            });

    private static void AddQueueCleanerJob(
        this IServiceCollectionQuartzConfigurator q,
        IConfiguration configuration,
        string trigger
    )
    {
        QueueCleanerConfig? config = configuration
            .GetRequiredSection(QueueCleanerConfig.SectionName)
            .Get<QueueCleanerConfig>();

        if (config is null)
        {
            throw new NullReferenceException($"{nameof(QueueCleaner)} configuration is null");
        }

        if (!config.Enabled)
        {
            return;
        }
        
        q.AddJob<QueueCleanerJob>(opts =>
        {
            opts.WithIdentity(nameof(QueueCleanerJob));
        });

        q.AddTrigger(opts =>
        {
            opts.ForJob(nameof(QueueCleanerJob))
                .WithIdentity($"{nameof(QueueCleanerJob)}-trigger")
                .WithCronSchedule(trigger, x =>x.WithMisfireHandlingInstructionDoNothing());
        });
    }

    private static void AddContentBlockerJob(
        this IServiceCollectionQuartzConfigurator q,
        IConfiguration configuration,
        string trigger
    )
    {
        ContentBlockerConfig? config = configuration
            .GetRequiredSection(ContentBlockerConfig.SectionName)
            .Get<ContentBlockerConfig>();

        if (config is null)
        {
            throw new NullReferenceException($"{nameof(ContentBlocker)} configuration is null");
        }

        if (!config.Enabled)
        {
            return;
        }

        q.AddJob<ContentBlockerJob>(opts =>
        {
            opts.WithIdentity(nameof(ContentBlockerJob));
        });

        q.AddTrigger(opts =>
        {
            opts.ForJob(nameof(ContentBlockerJob))
                .WithIdentity($"{nameof(ContentBlockerJob)}-trigger")
                .WithCronSchedule(trigger, x =>x.WithMisfireHandlingInstructionDoNothing());
        });
    }
}