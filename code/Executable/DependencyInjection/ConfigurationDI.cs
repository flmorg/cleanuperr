using Common.Configuration.Arr;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.General;
using Common.Configuration.Logging;
using Common.Configuration.QueueCleaner;

namespace Executable.DependencyInjection;

public static class ConfigurationDI
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration) =>
        services
            .Configure<DryRunConfig>(configuration)
            .Configure<QueueCleanerConfig>(configuration.GetSection(QueueCleanerConfig.SectionName))
            .Configure<ContentBlockerConfig>(configuration.GetSection(ContentBlockerConfig.SectionName))
            .Configure<DownloadCleanerConfig>(configuration.GetSection(DownloadCleanerConfig.SectionName))
            .Configure<DownloadClientConfig>(configuration)
            .Configure<QBitConfig>(configuration.GetSection(QBitConfig.SectionName))
            .Configure<DelugeConfig>(configuration.GetSection(DelugeConfig.SectionName))
            .Configure<TransmissionConfig>(configuration.GetSection(TransmissionConfig.SectionName))
            .Configure<SonarrConfig>(configuration.GetSection(SonarrConfig.SectionName))
            .Configure<RadarrConfig>(configuration.GetSection(RadarrConfig.SectionName))
            .Configure<LidarrConfig>(configuration.GetSection(LidarrConfig.SectionName))
            .Configure<LoggingConfig>(configuration.GetSection(LoggingConfig.SectionName));
}