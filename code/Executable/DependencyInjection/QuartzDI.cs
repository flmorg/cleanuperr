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
        q.AddJob<QueueCleaner, QueueCleanerConfig>(
            configuration,
            QueueCleanerConfig.SectionName,
            triggersConfig.QueueCleaner
        );
        
        q.AddJob<ContentBlocker, ContentBlockerConfig>(
            configuration,
            ContentBlockerConfig.SectionName,
            triggersConfig.ContentBlocker
        );
    }
    
    private static void AddJob<T, TConfig>(
        this IServiceCollectionQuartzConfigurator q,
        IConfiguration configuration,
        string configSectionName,
        string trigger
    )
        where T: GenericHandler
        where TConfig : IJobConfig
    {
        IJobConfig? config = configuration
            .GetRequiredSection(configSectionName)
            .Get<TConfig>();
        
        string typeName = typeof(T).Name;
        
        if (config is null)
        {
            throw new NullReferenceException($"{typeName} configuration is null");
        }

        if (!config.Enabled)
        {
            return;
        }

        q.AddJob<GenericJob<T>>(opts =>
        {
            opts.WithIdentity(typeName);
        });

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