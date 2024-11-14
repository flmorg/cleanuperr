using System.Net;
using Common.Configuration;
using Common.Configuration.ContentBlocker;
using Common.Configuration.QueueCleaner;
using Executable.Jobs;
using Infrastructure.Helpers;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.DownloadClient.Deluge;
using Infrastructure.Verticals.QueueCleaner;

namespace Executable;
using Quartz;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddLogging(builder => builder.ClearProviders().AddConsole())
            .AddHttpClients()
            .AddConfiguration(configuration)
            .AddServices()
            .AddQuartzServices(configuration);

    private static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        // add default HttpClient
        services.AddHttpClient();
        
        // add Deluge HttpClient
        services
            .AddHttpClient(nameof(DelugeClient), x =>
            {
                x.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(x =>
            {
                return new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    UseCookies = true,
                    CookieContainer = new CookieContainer(),
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
            });

        return services;
    }
    
    private static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration) =>
        services
            .Configure<ContentBlockerConfig>(configuration.GetSection(ContentBlockerConfig.SectionName))
            .Configure<QBitConfig>(configuration.GetSection(QBitConfig.SectionName))
            .Configure<DelugeConfig>(configuration.GetSection(DelugeConfig.SectionName))
            .Configure<SonarrConfig>(configuration.GetSection(SonarrConfig.SectionName))
            .Configure<RadarrConfig>(configuration.GetSection(RadarrConfig.SectionName));

    private static IServiceCollection AddServices(this IServiceCollection services) =>
        services
            .AddTransient<SonarrClient>()
            .AddTransient<RadarrClient>()
            .AddTransient<QueueCleanerJob>()
            .AddTransient<ContentBlockerJob>()
            .AddTransient<QueueCleaner>()
            .AddTransient<ContentBlocker>()
            .AddTransient<FilenameEvaluator>()
            .AddTransient<DelugeClient>()
            .AddTransient<QBitClient>()
            .AddTransient<ArrQueueIterator>()
            .AddTransient<DownloadClientFactory>();

    private static IServiceCollection AddQuartzServices(this IServiceCollection services, IConfiguration configuration) =>
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
                .WithCronSchedule(trigger);
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
                .WithCronSchedule(trigger);
        });
    }
}