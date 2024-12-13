using System.Net;
using Common.Configuration;
using Common.Configuration.ContentBlocker;
using Executable.Jobs;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.DownloadClient.Deluge;
using Infrastructure.Verticals.DownloadClient.QBittorrent;
using Infrastructure.Verticals.DownloadClient.Transmission;
using Infrastructure.Verticals.QueueCleaner;

namespace Executable.DependencyInjection;

public static class MainDI
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddLogging(builder => builder.ClearProviders().AddConsole())
            .AddHttpClients()
            .AddConfiguration(configuration)
            .AddMemoryCache()
            .AddServices()
            .AddQuartzServices(configuration);
    
    private static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        // add default HttpClient
        services.AddHttpClient();

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
            });

        return services;
    }
}