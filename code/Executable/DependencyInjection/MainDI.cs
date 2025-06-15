using System.Net;
using Data.Models.Configuration.General;
using Data.Models.Arr;
using Infrastructure.Health;
using Infrastructure.Http;
using Infrastructure.Verticals.DownloadClient.Deluge;
using Infrastructure.Verticals.DownloadClient.QBittorrent;
using Infrastructure.Verticals.DownloadClient.Transmission;
using Infrastructure.Verticals.DownloadRemover.Consumers;
using Infrastructure.Verticals.Notifications.Consumers;
using Infrastructure.Verticals.Notifications.Models;
using MassTransit;
using Polly;
using Polly.Extensions.Http;

namespace Executable.DependencyInjection;

public static class MainDI
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddLogging(builder => builder.ClearProviders().AddConsole())
            .AddHttpClients(configuration)
            .AddMemoryCache(options => {
                options.ExpirationScanFrequency = TimeSpan.FromMinutes(1);
            })
            .AddServices()
            .AddDownloadClientServices()
            .AddHealthServices()
            .AddQuartzServices(configuration)
            .AddNotifications(configuration)
            .AddMassTransit(config =>
            {
                config.AddConsumer<DownloadRemoverConsumer<SearchItem>>();
                config.AddConsumer<DownloadRemoverConsumer<SonarrSearchItem>>();
                
                config.AddConsumer<NotificationConsumer<FailedImportStrikeNotification>>();
                config.AddConsumer<NotificationConsumer<StalledStrikeNotification>>();
                config.AddConsumer<NotificationConsumer<SlowStrikeNotification>>();
                config.AddConsumer<NotificationConsumer<QueueItemDeletedNotification>>();
                config.AddConsumer<NotificationConsumer<DownloadCleanedNotification>>();
                config.AddConsumer<NotificationConsumer<CategoryChangedNotification>>();

                config.UsingInMemory((context, cfg) =>
                {
                    cfg.ReceiveEndpoint("download-remover-queue", e =>
                    {
                        e.ConfigureConsumer<DownloadRemoverConsumer<SearchItem>>(context);
                        e.ConfigureConsumer<DownloadRemoverConsumer<SonarrSearchItem>>(context);
                        e.ConcurrentMessageLimit = 1;
                        e.PrefetchCount = 1;
                    });
                    
                    cfg.ReceiveEndpoint("notification-queue", e =>
                    {
                        e.ConfigureConsumer<NotificationConsumer<FailedImportStrikeNotification>>(context);
                        e.ConfigureConsumer<NotificationConsumer<StalledStrikeNotification>>(context);
                        e.ConfigureConsumer<NotificationConsumer<SlowStrikeNotification>>(context);
                        e.ConfigureConsumer<NotificationConsumer<QueueItemDeletedNotification>>(context);
                        e.ConfigureConsumer<NotificationConsumer<DownloadCleanedNotification>>(context);
                        e.ConfigureConsumer<NotificationConsumer<CategoryChangedNotification>>(context);
                        e.ConcurrentMessageLimit = 1;
                        e.PrefetchCount = 1;
                    });
                });
            });
    
    private static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        // add default HttpClient
        services.AddHttpClient();
        
        // add dynamic HTTP client provider
        services.AddSingleton<IDynamicHttpClientProvider, DynamicHttpClientProvider>();
        
        // var configManager = services.BuildServiceProvider().GetRequiredService<IConfigManager>();
        // HttpConfig config = configManager.GetConfiguration<HttpConfig>("http.json") ?? new();
        // config.Validate();
        //
        // // add retry HttpClient
        // services
        //     .AddHttpClient(Constants.HttpClientWithRetryName, x =>
        //     {
        //         x.Timeout = TimeSpan.FromSeconds(config.Timeout);
        //     })
        //     .ConfigurePrimaryHttpMessageHandler(provider =>
        //     {
        //         CertificateValidationService service = provider.GetRequiredService<CertificateValidationService>();
        //         
        //         return new HttpClientHandler
        //         {
        //             ServerCertificateCustomValidationCallback = service.ShouldByPassValidationError
        //         };
        //     })
        //     .AddRetryPolicyHandler(config);
        //
        // // Note: We're no longer configuring specific named HttpClients for each download service
        // // Instead, we use the DynamicHttpClientProvider to create HttpClients as needed based on client configurations
        
        return services;
    }

    private static IHttpClientBuilder AddRetryPolicyHandler(this IHttpClientBuilder builder, GeneralConfig config) =>
        builder.AddPolicyHandler(
            HttpPolicyExtensions
                .HandleTransientHttpError()
                // do not retry on Unauthorized
                .OrResult(response => !response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Unauthorized)
                .WaitAndRetryAsync(config.HttpMaxRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
        );
        
    private static IServiceCollection AddDownloadClientServices(this IServiceCollection services) =>
        services
            // Register all download client service types
            // The factory will create instances as needed based on the client configuration
            .AddTransient<QBitService>()
            .AddTransient<TransmissionService>()
            .AddTransient<DelugeService>();
            
    /// <summary>
    /// Adds health check services to the service collection
    /// </summary>
    private static IServiceCollection AddHealthServices(this IServiceCollection services) =>
        services
            // Register the health check service
            .AddSingleton<IHealthCheckService, HealthCheckService>()
            
            // Register the background service for periodic health checks
            .AddHostedService<HealthCheckBackgroundService>();
}