using Common.Configuration.Arr;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.General;
using Common.Configuration.Logging;
using Common.Configuration.QueueCleaner;
using Infrastructure.Configuration;
using Infrastructure.Services;
using System.IO;

namespace Executable.DependencyInjection;

public static class ConfigurationDI
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options from appsettings.json
        services
            .Configure<DryRunConfig>(configuration)
            .Configure<SearchConfig>(configuration)
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

        // Add JSON-based configuration services
        string configDirectory = Path.Combine(
            Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? AppDomain.CurrentDomain.BaseDirectory,
            "config");
            
        services.AddConfigurationServices(configDirectory);
        
        return services;
    }
}