using System.Net;
using Common.Configuration.General;
using Common.Helpers;
using Infrastructure.Verticals.DownloadClient.Deluge;
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
            .AddConfiguration(configuration)
            .AddMemoryCache()
            .AddServices()
            .AddQuartzServices(configuration)
            .AddNotifications(configuration)
            .AddMassTransit(config =>
            {
                config.AddConsumer<NotificationConsumer<FailedImportStrikeNotification>>();
                config.AddConsumer<NotificationConsumer<StalledStrikeNotification>>();
                config.AddConsumer<NotificationConsumer<QueueItemDeleteNotification>>();

                config.UsingInMemory((context, cfg) =>
                {
                    cfg.ReceiveEndpoint("notification-queue", e =>
                    {
                        e.ConfigureConsumer<NotificationConsumer<FailedImportStrikeNotification>>(context);
                        e.ConfigureConsumer<NotificationConsumer<StalledStrikeNotification>>(context);
                        e.ConfigureConsumer<NotificationConsumer<QueueItemDeleteNotification>>(context);
                        e.ConcurrentMessageLimit = 1;
                        e.PrefetchCount = 1;
                    });
                });
            });
    
    private static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        // add default HttpClient
        services.AddHttpClient();
        
        HttpConfig config = configuration.Get<HttpConfig>() ?? new();
        config.Validate();

        // add retry HttpClient
        services
            .AddHttpClient(Constants.HttpClientWithRetryName, x =>
            {
                x.Timeout = TimeSpan.FromSeconds(config.Timeout);
            })
            .AddRetryPolicyHandler(config);

        // add Deluge HttpClient
        services
            .AddHttpClient(nameof(DelugeService), x =>
            {
                x.Timeout = TimeSpan.FromSeconds(5);
            })
            .ConfigurePrimaryHttpMessageHandler(_ =>
            {
                return new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    UseCookies = true,
                    CookieContainer = new CookieContainer(),
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
            })
            .AddRetryPolicyHandler(config);

        return services;
    }

    private static IHttpClientBuilder AddRetryPolicyHandler(this IHttpClientBuilder builder, HttpConfig config) =>
        builder.AddPolicyHandler(
            HttpPolicyExtensions
                .HandleTransientHttpError()
                // do not retry on Unauthorized
                .OrResult(response => !response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Unauthorized)
                .WaitAndRetryAsync(config.MaxRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
        );
}