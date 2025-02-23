using Infrastructure.Interceptors;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.DownloadCleaner;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.DownloadClient.Deluge;
using Infrastructure.Verticals.DownloadClient.QBittorrent;
using Infrastructure.Verticals.DownloadClient.Transmission;
using Infrastructure.Verticals.ItemStriker;
using Infrastructure.Verticals.QueueCleaner;

namespace Executable.DependencyInjection;

public static class ServicesDI
{
    public static IServiceCollection AddServices(this IServiceCollection services) =>
        services
            .AddTransient<IDryRunInterceptor, DryRunInterceptor>()
            .AddTransient<SonarrClient>()
            .AddTransient<RadarrClient>()
            .AddTransient<LidarrClient>()
            .AddTransient<QueueCleaner>()
            .AddTransient<ContentBlocker>()
            .AddTransient<DownloadCleaner>()
            .AddTransient<IFilenameEvaluator, FilenameEvaluator>()
            .AddTransient<DummyDownloadService>()
            .AddTransient<QBitService>()
            .AddTransient<DelugeService>()
            .AddTransient<TransmissionService>()
            .AddTransient<ArrQueueIterator>()
            .AddTransient<DownloadServiceFactory>()
            .AddSingleton<BlocklistProvider>()
            .AddSingleton<IStriker, Striker>();
}