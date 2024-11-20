using Executable.Jobs;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.DownloadClient.Deluge;
using Infrastructure.Verticals.DownloadClient.QBittorrent;
using Infrastructure.Verticals.DownloadClient.Transmission;
using Infrastructure.Verticals.QueueCleaner;

namespace Executable.DependencyInjection;

public static class ServicesDI
{
    public static IServiceCollection AddServices(this IServiceCollection services) =>
        services
            .AddTransient<SonarrClient>()
            .AddTransient<RadarrClient>()
            .AddTransient<QueueCleaner>()
            .AddTransient<ContentBlocker>()
            .AddTransient<FilenameEvaluator>()
            .AddTransient<QBitService>()
            .AddTransient<DelugeService>()
            .AddTransient<TransmissionService>()
            .AddTransient<ArrQueueIterator>()
            .AddTransient<DownloadServiceFactory>()
            .AddSingleton<BlocklistProvider>();
}