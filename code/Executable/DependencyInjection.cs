using Common.Configuration;
using Executable.Jobs;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.QueueCleaner;

namespace Executable;
using Quartz;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddLogging(builder => builder.ClearProviders().AddConsole())
            .AddHttpClient()
            .AddConfiguration(configuration)
            .AddServices()
            .AddQuartzServices(configuration);

    private static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration) =>
        services
            .Configure<QBitConfig>(configuration.GetSection(QBitConfig.SectionName))
            .Configure<SonarrConfig>(configuration.GetSection(SonarrConfig.SectionName))
            .Configure<RadarrConfig>(configuration.GetSection(RadarrConfig.SectionName));

    private static IServiceCollection AddServices(this IServiceCollection services) =>
        services
            .AddTransient<SonarrClient>()
            .AddTransient<RadarrClient>()
            .AddTransient<QueueCleanerJob>()
            .AddTransient<QueueCleanerHandler>();

    private static IServiceCollection AddQuartzServices(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddQuartz(q =>
            {
                TriggersConfig? config = configuration.GetRequiredSection(TriggersConfig.SectionName).Get<TriggersConfig>();

                if (config is null)
                {
                    throw new NullReferenceException("Quartz configuration is null");
                }

                q.AddQueueCleanerJob(config.QueueCleaner);
            })
            .AddQuartzHostedService(opt =>
            {
                opt.WaitForJobsToComplete = true;
            });

    private static void AddQueueCleanerJob(this IServiceCollectionQuartzConfigurator q, string trigger)
    {
        q.AddJob<QueueCleanerJob>(opts =>
        {
            opts.WithIdentity(nameof(QueueCleanerJob));
        });

        q.AddTrigger(opts =>
        {
            opts.ForJob(nameof(QueueCleanerJob))
                .WithIdentity($"{nameof(QueueCleanerJob)}-trigger")
                .WithCronSchedule(trigger);
        });
    }
}