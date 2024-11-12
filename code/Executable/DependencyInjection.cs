using Common.Configuration;
using Executable.Jobs;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.BlockedTorrent;

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
            .Configure<QuartzConfig>(configuration.GetSection(nameof(QuartzConfig)))
            .Configure<QBitConfig>(configuration.GetSection(nameof(QBitConfig)))
            .Configure<SonarrConfig>(configuration.GetSection(nameof(SonarrConfig)))
            .Configure<RadarrConfig>(configuration.GetSection(nameof(RadarrConfig)));

    private static IServiceCollection AddServices(this IServiceCollection services) =>
        services
            .AddTransient<SonarrClient>()
            .AddTransient<RadarrClient>()
            .AddTransient<BlockedTorrentJob>()
            .AddTransient<BlockedTorrentHandler>();

    private static IServiceCollection AddQuartzServices(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddQuartz(q =>
            {
                QuartzConfig? config = configuration.GetRequiredSection(nameof(QuartzConfig)).Get<QuartzConfig>();

                if (config is null)
                {
                    throw new NullReferenceException("Quartz configuration is null");
                }

                q.AddBlockedTorrentJob(config.BlockedTorrentTrigger);
            })
            .AddQuartzHostedService(opt =>
            {
                opt.WaitForJobsToComplete = true;
            });

    private static void AddBlockedTorrentJob(this IServiceCollectionQuartzConfigurator q, string trigger)
    {
        q.AddJob<BlockedTorrentJob>(opts =>
        {
            opts.WithIdentity(nameof(BlockedTorrentJob));
        });

        q.AddTrigger(opts =>
        {
            opts.ForJob(nameof(BlockedTorrentJob))
                .WithIdentity($"{nameof(BlockedTorrentJob)}-trigger")
                .WithCronSchedule(trigger);
        });
    }
}