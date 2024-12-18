﻿using Common.Configuration;
using Common.Configuration.Arr;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadClient;
using Common.Configuration.Logging;
using Common.Configuration.QueueCleaner;
using Domain.Enums;

namespace Executable.DependencyInjection;

public static class ConfigurationDI
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration) =>
        services
            .Configure<QueueCleanerConfig>(configuration.GetSection(QueueCleanerConfig.SectionName))
            .Configure<ContentBlockerConfig>(configuration.GetSection(ContentBlockerConfig.SectionName))
            .Configure<QBitConfig>(configuration.GetSection(QBitConfig.SectionName))
            .Configure<DelugeConfig>(configuration.GetSection(DelugeConfig.SectionName))
            .Configure<TransmissionConfig>(configuration.GetSection(TransmissionConfig.SectionName))
            .Configure<SonarrConfig>(configuration.GetSection(SonarrConfig.SectionName))
            .Configure<RadarrConfig>(configuration.GetSection(RadarrConfig.SectionName))
            .Configure<LoggingConfig>(configuration.GetSection(LoggingConfig.SectionName));
}