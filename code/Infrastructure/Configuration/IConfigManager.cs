using Common.Configuration.Arr;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.General;
using Common.Configuration.IgnoredDownloads;
using Common.Configuration.Notification;
using Common.Configuration.QueueCleaner;
using Infrastructure.Verticals.Notifications.Notifiarr;

namespace Infrastructure.Configuration;

/// <summary>
/// Provides configuration management for various components with thread-safe file access.
/// </summary>
public interface IConfigManager
{
    // Configuration files - Async methods
    Task<T?> GetConfigurationAsync<T>(string configFileName) where T : class, new();
    Task<bool> SaveConfigurationAsync<T>(string configFileName, T config) where T : class;
    Task<bool> UpdateConfigurationPropertyAsync<T>(string configFileName, string propertyPath, T value);
    Task<bool> MergeConfigurationAsync<T>(string configFileName, T newValues) where T : class;
    Task<bool> DeleteConfigurationAsync(string configFileName);
    IEnumerable<string> ListConfigurationFiles();
    
    // Configuration files - Sync methods
    T? GetConfiguration<T>(string configFileName) where T : class, new();
    bool SaveConfiguration<T>(string configFileName, T config) where T : class;
    bool UpdateConfigurationProperty<T>(string configFileName, string propertyPath, T value);
    bool MergeConfiguration<T>(string configFileName, T newValues) where T : class;
    bool DeleteConfiguration(string configFileName);

    // Specific configuration types - Async methods
    Task<GeneralConfig?> GetGeneralConfigAsync();
    Task<SonarrConfig?> GetSonarrConfigAsync();
    Task<RadarrConfig?> GetRadarrConfigAsync();
    Task<LidarrConfig?> GetLidarrConfigAsync();
    Task<ContentBlockerConfig?> GetContentBlockerConfigAsync();
    Task<QueueCleanerConfig?> GetQueueCleanerConfigAsync();
    Task<DownloadCleanerConfig?> GetDownloadCleanerConfigAsync();
    Task<DownloadClientConfig?> GetDownloadClientConfigAsync();
    Task<IgnoredDownloadsConfig?> GetIgnoredDownloadsConfigAsync();
    Task<NotificationsConfig?> GetNotificationsConfigAsync();

    Task<bool> SaveGeneralConfigAsync(GeneralConfig config);
    Task<bool> SaveSonarrConfigAsync(SonarrConfig config);
    Task<bool> SaveRadarrConfigAsync(RadarrConfig config);
    Task<bool> SaveLidarrConfigAsync(LidarrConfig config);
    Task<bool> SaveContentBlockerConfigAsync(ContentBlockerConfig config);
    Task<bool> SaveQueueCleanerConfigAsync(QueueCleanerConfig config);
    Task<bool> SaveDownloadCleanerConfigAsync(DownloadCleanerConfig config);
    Task<bool> SaveDownloadClientConfigAsync(DownloadClientConfig config);
    Task<bool> SaveIgnoredDownloadsConfigAsync(IgnoredDownloadsConfig config);
    Task<bool> SaveNotificationsConfigAsync(NotificationsConfig config);
    
    // Specific configuration types - Sync methods
    GeneralConfig? GetGeneralConfig();
    SonarrConfig? GetSonarrConfig();
    RadarrConfig? GetRadarrConfig();
    LidarrConfig? GetLidarrConfig();
    ContentBlockerConfig? GetContentBlockerConfig();
    QueueCleanerConfig? GetQueueCleanerConfig();
    DownloadCleanerConfig? GetDownloadCleanerConfig();
    DownloadClientConfig? GetDownloadClientConfig();
    IgnoredDownloadsConfig? GetIgnoredDownloadsConfig();
    NotificationsConfig? GetNotificationsConfig();
    
    bool SaveGeneralConfig(GeneralConfig config);
    bool SaveSonarrConfig(SonarrConfig config);
    bool SaveRadarrConfig(RadarrConfig config);
    bool SaveLidarrConfig(LidarrConfig config);
    bool SaveContentBlockerConfig(ContentBlockerConfig config);
    bool SaveQueueCleanerConfig(QueueCleanerConfig config);
    bool SaveDownloadCleanerConfig(DownloadCleanerConfig config);
    bool SaveDownloadClientConfig(DownloadClientConfig config);
    bool SaveIgnoredDownloadsConfig(IgnoredDownloadsConfig config);
    bool SaveNotificationsConfig(NotificationsConfig config);
}