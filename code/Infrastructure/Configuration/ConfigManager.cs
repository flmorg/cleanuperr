using Common.Configuration;
using Common.Configuration.Arr;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.General;
using Common.Configuration.IgnoredDownloads;
using Common.Configuration.Notification;
using Common.Configuration.QueueCleaner;
using Common.Helpers;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Configuration;

public class ConfigManager : IConfigManager
{
    private readonly ILogger<ConfigManager> _logger;
    private readonly IConfigurationProvider _configProvider;

    // Define standard config file names with cross-platform paths
    private readonly string _generalConfigFile;
    private readonly string _sonarrConfigFile;
    private readonly string _radarrConfigFile;
    private readonly string _lidarrConfigFile;
    private readonly string _contentBlockerConfigFile;
    private readonly string _queueCleanerConfigFile;
    private readonly string _downloadCleanerConfigFile;
    private readonly string _downloadClientConfigFile;
    private readonly string _ignoredDownloadsConfigFile;
    private readonly string _notificationsConfigFile;

    private readonly Dictionary<Type, string> _settingsPaths;

    public ConfigManager(
        ILogger<ConfigManager> logger,
        IConfigurationProvider configProvider)
    {
        _logger = logger;
        _configProvider = configProvider;
        string settingsPath = ConfigurationPathProvider.GetSettingsPath();
        
        // _generalConfigFile = Path.Combine(settingsPath, "general.json");
        // _sonarrConfigFile = Path.Combine(settingsPath, "sonarr.json");
        // _radarrConfigFile = Path.Combine(settingsPath, "radarr.json");
        // _lidarrConfigFile = Path.Combine(settingsPath, "lidarr.json");
        // _contentBlockerConfigFile = Path.Combine(settingsPath, "content_blocker.json");
        // _queueCleanerConfigFile = Path.Combine(settingsPath, "queue_cleaner.json");
        // _downloadCleanerConfigFile = Path.Combine(settingsPath, "download_cleaner.json");
        // _downloadClientConfigFile = Path.Combine(settingsPath, "download_client.json");
        // _ignoredDownloadsConfigFile = Path.Combine(settingsPath, "ignored_downloads.json");
        // _notificationsConfigFile = Path.Combine(settingsPath, "notifications.json");
        
        _settingsPaths = new()
        {
            { typeof(GeneralConfig), "general.json" },
            { typeof(SonarrConfig), "sonarr.json" },
            { typeof(RadarrConfig), "radarr.json" },
            { typeof(LidarrConfig), "lidarr.json" },
            { typeof(ContentBlockerConfig), "content_blocker.json" },
            { typeof(QueueCleanerConfig), "queue_cleaner.json" },
            { typeof(DownloadCleanerConfig), "download_cleaner.json" },
            { typeof(DownloadClientConfig), "download_client.json" },
            { typeof(IgnoredDownloadsConfig), "ignored_downloads.json" },
            { typeof(NotificationsConfig), "notifications.json" }
        };
    }

    public async Task EnsureFilesExist()
    {
        foreach ((Type type, string path) in _settingsPaths)
        {
            try
            {
                if (_configProvider.FileExists(path))
                {
                    continue;
                }
                
                var config = Activator.CreateInstance(type);

                if (config is null)
                {
                    throw new InvalidOperationException($"Failed to create instance of {type}");
                }
                
                // Create the file with default values
                await _configProvider.WriteConfigurationAsync(path, config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure configuration file exists: {path}", path);
                throw;
            }
        }
    }

    // Generic configuration methods
    // public Task<T> GetConfigurationAsync<T>(string configFileName) where T : class, new()
    // {
    //     return _configProvider.ReadConfigurationAsync<T>(configFileName);
    // }
    
    public Task<T> GetConfigurationAsync<T>() where T : class, new()
    {
        return _configProvider.ReadConfigurationAsync<T>(_settingsPaths[typeof(T)]);
    }

    // public Task<bool> SaveConfigurationAsync<T>(string configFileName, T config) where T : class
    // {
    //     // Validate if it's an IConfig
    //     if (config is IConfig configurable)
    //     {
    //         try
    //         {
    //             configurable.Validate();
    //         }
    //         catch (Exception ex)
    //         {
    //             _logger.LogError(ex, "Configuration validation failed for {fileName}", configFileName);
    //             return Task.FromResult(false);
    //         }
    //     }
    //
    //     return _configProvider.WriteConfigurationAsync(configFileName, config);
    // }
    
    public Task<bool> SaveConfigurationAsync<T>(T config) where T : class
    {
        string configFileName = _settingsPaths[typeof(T)];
        
        // Validate if it's an IConfig
        if (config is IConfig configurable)
        {
            try
            {
                configurable.Validate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Configuration validation failed for {fileName}", configFileName);
                return Task.FromResult(false);
            }
        }

        return _configProvider.WriteConfigurationAsync(configFileName, config);
    }

    public Task<bool> UpdateConfigurationPropertyAsync<T>(string configFileName, string propertyPath, T value)
    {
        return _configProvider.UpdateConfigurationPropertyAsync(configFileName, propertyPath, value);
    }

    public Task<bool> MergeConfigurationAsync<T>(string configFileName, T newValues) where T : class
    {
        return _configProvider.MergeConfigurationAsync(configFileName, newValues);
    }

    public Task<bool> DeleteConfigurationAsync(string configFileName)
    {
        return _configProvider.DeleteConfigurationAsync(configFileName);
    }

    public IEnumerable<string> ListConfigurationFiles()
    {
        return _configProvider.ListConfigurationFiles();
    }

    // Specific configuration type methods
    // public async Task<GeneralConfig> GetGeneralConfigAsync()
    // {
    //     return await _configProvider.ReadConfigurationAsync<GeneralConfig>(_generalConfigFile);
    // }
    //
    // public async Task<SonarrConfig> GetSonarrConfigAsync()
    // {
    //     return await _configProvider.ReadConfigurationAsync<SonarrConfig>(_sonarrConfigFile);
    // }
    //
    // public async Task<RadarrConfig> GetRadarrConfigAsync()
    // {
    //     return await _configProvider.ReadConfigurationAsync<RadarrConfig>(_radarrConfigFile);
    // }
    //
    // public async Task<LidarrConfig> GetLidarrConfigAsync()
    // {
    //     return await _configProvider.ReadConfigurationAsync<LidarrConfig>(_lidarrConfigFile);
    // }
    //
    // public async Task<ContentBlockerConfig> GetContentBlockerConfigAsync()
    // {
    //     return await _configProvider.ReadConfigurationAsync<ContentBlockerConfig>(_contentBlockerConfigFile);
    // }
    //
    // public async Task<NotificationsConfig> GetNotificationsConfigAsync()
    // {
    //     return await _configProvider.ReadConfigurationAsync<NotificationsConfig>(_notificationsConfigFile);
    // }
    //
    // public async Task<QueueCleanerConfig> GetQueueCleanerConfigAsync()
    // {
    //     return await _configProvider.ReadConfigurationAsync<QueueCleanerConfig>(_queueCleanerConfigFile);
    // }
    //
    // public async Task<DownloadCleanerConfig> GetDownloadCleanerConfigAsync()
    // {
    //     return await _configProvider.ReadConfigurationAsync<DownloadCleanerConfig>(_downloadCleanerConfigFile);
    // }
    //
    // public async Task<DownloadClientConfig> GetDownloadClientConfigAsync()
    // {
    //     return await _configProvider.ReadConfigurationAsync<DownloadClientConfig>(_downloadClientConfigFile);
    // }
    //
    // public async Task<IgnoredDownloadsConfig> GetIgnoredDownloadsConfigAsync()
    // {
    //     return await _configProvider.ReadConfigurationAsync<IgnoredDownloadsConfig>(_ignoredDownloadsConfigFile);
    // }

    // public Task<bool> SaveGeneralConfigAsync(GeneralConfig config)
    // {
    //     try
    //     {
    //         config.Validate();
    //         return _configProvider.WriteConfigurationAsync(_generalConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "General configuration validation failed");
    //         return Task.FromResult(false);
    //     }
    // }
    //
    // public Task<bool> SaveSonarrConfigAsync(SonarrConfig config)
    // {
    //     try
    //     {
    //         config.Validate();
    //         return _configProvider.WriteConfigurationAsync(_sonarrConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Sonarr configuration validation failed");
    //         return Task.FromResult(false);
    //     }
    // }
    //
    // public Task<bool> SaveRadarrConfigAsync(RadarrConfig config)
    // {
    //     try
    //     {
    //         config.Validate();
    //         return _configProvider.WriteConfigurationAsync(_radarrConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Radarr configuration validation failed");
    //         return Task.FromResult(false);
    //     }
    // }
    //
    // public Task<bool> SaveLidarrConfigAsync(LidarrConfig config)
    // {
    //     try
    //     {
    //         config.Validate();
    //         return _configProvider.WriteConfigurationAsync(_lidarrConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Lidarr configuration validation failed");
    //         return Task.FromResult(false);
    //     }
    // }
    //
    // public Task<bool> SaveContentBlockerConfigAsync(ContentBlockerConfig config)
    // {
    //     try
    //     {
    //         config.Validate();
    //         return _configProvider.WriteConfigurationAsync(_contentBlockerConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "ContentBlocker configuration validation failed");
    //         return Task.FromResult(false);
    //     }
    // }
    //
    // public Task<bool> SaveQueueCleanerConfigAsync(QueueCleanerConfig config)
    // {
    //     try
    //     {
    //         config.Validate();
    //         return _configProvider.WriteConfigurationAsync(_queueCleanerConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "QueueCleaner configuration validation failed");
    //         return Task.FromResult(false);
    //     }
    // }
    //
    // public Task<bool> SaveDownloadCleanerConfigAsync(DownloadCleanerConfig config)
    // {
    //     try
    //     {
    //         config.Validate();
    //         return _configProvider.WriteConfigurationAsync(_downloadCleanerConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "DownloadCleaner configuration validation failed");
    //         return Task.FromResult(false);
    //     }
    // }
    //
    // public Task<bool> SaveDownloadClientConfigAsync(DownloadClientConfig config)
    // {
    //     try
    //     {
    //         config.Validate();
    //         return _configProvider.WriteConfigurationAsync(_downloadClientConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "DownloadClient configuration validation failed");
    //         return Task.FromResult(false);
    //     }
    // }
    //
    // public Task<bool> SaveIgnoredDownloadsConfigAsync(IgnoredDownloadsConfig config)
    // {
    //     try
    //     {
    //         return _configProvider.WriteConfigurationAsync(_ignoredDownloadsConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "IgnoredDownloads configuration save failed");
    //         return Task.FromResult(false);
    //     }
    // }
    //
    // public Task<bool> SaveNotificationsConfigAsync(NotificationsConfig config)
    // {
    //     try
    //     {
    //         return _configProvider.WriteConfigurationAsync(_notificationsConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Notifications configuration save failed");
    //         return Task.FromResult(false);
    //     }
    // }
    
    // // Generic synchronous configuration methods
    // public T GetConfiguration<T>(string fileName) where T : class, new()
    // {
    //     return _configProvider.ReadConfiguration<T>(fileName);
    // }

    public T GetConfiguration<T>() where T : class, new()
    {
        return _configProvider.ReadConfiguration<T>(_settingsPaths[typeof(T)]);
    }
    
    // public bool SaveConfiguration<T>(string configFileName, T config) where T : class
    // {
    //     // Validate if it's an IConfig
    //     if (config is IConfig configurable)
    //     {
    //         try
    //         {
    //             configurable.Validate();
    //         }
    //         catch (Exception ex)
    //         {
    //             _logger.LogError(ex, "Configuration validation failed for {fileName}", configFileName);
    //             return false;
    //         }
    //     }
    //
    //     return _configProvider.WriteConfiguration(configFileName, config);
    // }
    
    public bool SaveConfiguration<T>(T config) where T : class
    {
        string configFileName = _settingsPaths[typeof(T)];
        
        // Validate if it's an IConfig
        if (config is IConfig configurable)
        {
            try
            {
                configurable.Validate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Configuration validation failed for {fileName}", configFileName);
                return false;
            }
        }

        try
        {
            return _configProvider.WriteConfiguration(configFileName, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration save failed for {fileName}", configFileName);
            throw;
        }
    }
    
    public bool UpdateConfigurationProperty<T>(string configFileName, string propertyPath, T value)
    {
        return _configProvider.UpdateConfigurationProperty(configFileName, propertyPath, value);
    }
    
    public bool MergeConfiguration<T>(string configFileName, T newValues) where T : class
    {
        return _configProvider.MergeConfiguration(configFileName, newValues);
    }
    
    public bool DeleteConfiguration(string configFileName)
    {
        return _configProvider.DeleteConfiguration(configFileName);
    }
    
    // Specific synchronous configuration methods for typed configs
    
    // public GeneralConfig GetGeneralConfig()
    // {
    //     return _configProvider.ReadConfiguration<GeneralConfig>(_generalConfigFile);
    // }
    //
    // public SonarrConfig GetSonarrConfig()
    // {
    //     return _configProvider.ReadConfiguration<SonarrConfig>(_sonarrConfigFile);
    // }
    //
    // public RadarrConfig GetRadarrConfig()
    // {
    //     return _configProvider.ReadConfiguration<RadarrConfig>(_radarrConfigFile);
    // }
    //
    // public LidarrConfig GetLidarrConfig()
    // {
    //     return _configProvider.ReadConfiguration<LidarrConfig>(_lidarrConfigFile);
    // }
    //
    // public QueueCleanerConfig GetQueueCleanerConfig()
    // {
    //     return GetConfiguration<QueueCleanerConfig>(_queueCleanerConfigFile);
    // }
    //
    // public ContentBlockerConfig GetContentBlockerConfig()
    // {
    //     return _configProvider.ReadConfiguration<ContentBlockerConfig>(_contentBlockerConfigFile);
    // }
    //
    // public DownloadCleanerConfig GetDownloadCleanerConfig()
    // {
    //     return _configProvider.ReadConfiguration<DownloadCleanerConfig>(_downloadCleanerConfigFile);
    // }
    //
    // public DownloadClientConfig GetDownloadClientConfig()
    // {
    //     return _configProvider.ReadConfiguration<DownloadClientConfig>(_downloadClientConfigFile);
    // }
    //
    // public IgnoredDownloadsConfig GetIgnoredDownloadsConfig()
    // {
    //     return _configProvider.ReadConfiguration<IgnoredDownloadsConfig>(_ignoredDownloadsConfigFile);
    // }
    //
    // public NotificationsConfig GetNotificationsConfig()
    // {
    //     return _configProvider.ReadConfiguration<NotificationsConfig>(_notificationsConfigFile);
    // }
    
    // public bool SaveGeneralConfig(GeneralConfig config)
    // {
    //     try
    //     {
    //         config.Validate();
    //         return _configProvider.WriteConfiguration(_generalConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "General configuration validation failed");
    //         return false;
    //     }
    // }
    //
    // public bool SaveSonarrConfig(SonarrConfig config)
    // {
    //     try
    //     {
    //         config.Validate();
    //         return _configProvider.WriteConfiguration(_sonarrConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Sonarr configuration validation failed");
    //         return false;
    //     }
    // }
    //
    // public bool SaveRadarrConfig(RadarrConfig config)
    // {
    //     try
    //     {
    //         config.Validate();
    //         return _configProvider.WriteConfiguration(_radarrConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Radarr configuration validation failed");
    //         return false;
    //     }
    // }
    //
    // public bool SaveLidarrConfig(LidarrConfig config)
    // {
    //     try
    //     {
    //         config.Validate();
    //         return _configProvider.WriteConfiguration(_lidarrConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Lidarr configuration validation failed");
    //         return false;
    //     }
    // }
    //
    // public bool SaveContentBlockerConfig(ContentBlockerConfig config)
    // {
    //     try
    //     {
    //         config.Validate();
    //         return _configProvider.WriteConfiguration(_contentBlockerConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "ContentBlocker configuration validation failed");
    //         return false;
    //     }
    // }
    //
    // public bool SaveQueueCleanerConfig(QueueCleanerConfig config)
    // {
    //     try
    //     {
    //         config.Validate();
    //         return _configProvider.WriteConfiguration(_queueCleanerConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "QueueCleaner configuration validation failed");
    //         return false;
    //     }
    // }
    //
    // public bool SaveDownloadCleanerConfig(DownloadCleanerConfig config)
    // {
    //     try
    //     {
    //         config.Validate();
    //         return _configProvider.WriteConfiguration(_downloadCleanerConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "DownloadCleaner configuration validation failed");
    //         return false;
    //     }
    // }
    //
    // public bool SaveDownloadClientConfig(DownloadClientConfig config)
    // {
    //     try
    //     {
    //         config.Validate();
    //         return _configProvider.WriteConfiguration(_downloadClientConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "DownloadClient configuration validation failed");
    //         return false;
    //     }
    // }
    //
    // public bool SaveIgnoredDownloadsConfig(IgnoredDownloadsConfig config)
    // {
    //     try
    //     {
    //         return _configProvider.WriteConfiguration(_ignoredDownloadsConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "IgnoredDownloads configuration save failed");
    //         return false;
    //     }
    // }
    //
    // public bool SaveNotificationsConfig(NotificationsConfig config)
    // {
    //     try
    //     {
    //         return _configProvider.WriteConfiguration(_notificationsConfigFile, config);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Notifications configuration save failed");
    //         return false;
    //     }
    // }
}
