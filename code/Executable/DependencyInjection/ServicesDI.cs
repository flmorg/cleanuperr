using Data;
using Infrastructure.Events;
using Infrastructure.Interceptors;
using Infrastructure.Services;
using Infrastructure.Services.Interfaces;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.DownloadCleaner;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.DownloadClient.Deluge;
using Infrastructure.Verticals.DownloadClient.QBittorrent;
using Infrastructure.Verticals.DownloadClient.Transmission;
using Infrastructure.Verticals.DownloadRemover;
using Infrastructure.Verticals.DownloadRemover.Interfaces;
using Infrastructure.Verticals.Files;
using Infrastructure.Verticals.ItemStriker;
using Infrastructure.Verticals.QueueCleaner;
using Infrastructure.Verticals.Security;

namespace Executable.DependencyInjection;

public static class ServicesDI
{
    public static IServiceCollection AddServices(this IServiceCollection services) =>
        services
            .AddSingleton<IEncryptionService, AesEncryptionService>()
            .AddTransient<SensitiveDataJsonConverter>()
            .AddTransient<DataContext>()
            .AddTransient<EventPublisher>()
            .AddHostedService<EventCleanupService>()
            // API services
            .AddSingleton<IJobManagementService, JobManagementService>()
            // Core services
            .AddTransient<IDryRunInterceptor, DryRunInterceptor>()
            .AddTransient<CertificateValidationService>()
            .AddTransient<SonarrClient>()
            .AddTransient<RadarrClient>()
            .AddTransient<LidarrClient>()
            .AddTransient<ArrClientFactory>()
            .AddTransient<QueueCleaner>()
            .AddTransient<ContentBlocker>()
            .AddTransient<DownloadCleaner>()
            .AddTransient<IQueueItemRemover, QueueItemRemover>()
            .AddTransient<IFilenameEvaluator, FilenameEvaluator>()
            .AddTransient<IHardLinkFileService, HardLinkFileService>()
            .AddTransient<UnixHardLinkFileService>()
            .AddTransient<WindowsHardLinkFileService>()
            // Download client services
            .AddTransient<QBitService>()
            .AddTransient<DelugeService>()
            .AddTransient<TransmissionService>()
            .AddTransient<ArrQueueIterator>()
            .AddTransient<DownloadServiceFactory>()
            .AddTransient<IStriker, Striker>()
            .AddSingleton<BlocklistProvider>()
            .AddSingleton<IIgnoredDownloadsService, IgnoredDownloadsService>();
}