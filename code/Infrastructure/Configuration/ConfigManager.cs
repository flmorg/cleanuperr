using Common.Configuration;
using Common.Configuration.Arr;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.IgnoredDownloads;
using Common.Configuration.QueueCleaner;
using Microsoft.Extensions.Logging;

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
    Task<SonarrConfig?> GetSonarrConfigAsync();
    Task<RadarrConfig?> GetRadarrConfigAsync();
    Task<LidarrConfig?> GetLidarrConfigAsync();
    Task<ContentBlockerConfig?> GetContentBlockerConfigAsync();
    Task<QueueCleanerConfig?> GetQueueCleanerConfigAsync();
    Task<DownloadCleanerConfig?> GetDownloadCleanerConfigAsync();
    Task<DownloadClientConfig?> GetDownloadClientConfigAsync();
    Task<IgnoredDownloadsConfig?> GetIgnoredDownloadsConfigAsync();

    Task<bool> SaveSonarrConfigAsync(SonarrConfig config);
    Task<bool> SaveRadarrConfigAsync(RadarrConfig config);
    Task<bool> SaveLidarrConfigAsync(LidarrConfig config);
    Task<bool> SaveContentBlockerConfigAsync(ContentBlockerConfig config);
    Task<bool> SaveQueueCleanerConfigAsync(QueueCleanerConfig config);
    Task<bool> SaveDownloadCleanerConfigAsync(DownloadCleanerConfig config);
    Task<bool> SaveDownloadClientConfigAsync(DownloadClientConfig config);
    Task<bool> SaveIgnoredDownloadsConfigAsync(IgnoredDownloadsConfig config);
    
    // Specific configuration types - Sync methods
    SonarrConfig? GetSonarrConfig();
    RadarrConfig? GetRadarrConfig();
    LidarrConfig? GetLidarrConfig();
    ContentBlockerConfig? GetContentBlockerConfig();
    QueueCleanerConfig? GetQueueCleanerConfig();
    DownloadCleanerConfig? GetDownloadCleanerConfig();
    DownloadClientConfig? GetDownloadClientConfig();
    IgnoredDownloadsConfig? GetIgnoredDownloadsConfig();
    
    bool SaveSonarrConfig(SonarrConfig config);
    bool SaveRadarrConfig(RadarrConfig config);
    bool SaveLidarrConfig(LidarrConfig config);
    bool SaveContentBlockerConfig(ContentBlockerConfig config);
    bool SaveQueueCleanerConfig(QueueCleanerConfig config);
    bool SaveDownloadCleanerConfig(DownloadCleanerConfig config);
    bool SaveDownloadClientConfig(DownloadClientConfig config);
    bool SaveIgnoredDownloadsConfig(IgnoredDownloadsConfig config);
}

public class ConfigManager : IConfigManager
{
    private readonly ILogger<ConfigManager> _logger;
    private readonly JsonConfigurationProvider _configProvider;

    // Define standard config file names
    private const string SonarrConfigFile = "sonarr.json";
    private const string RadarrConfigFile = "radarr.json";
    private const string LidarrConfigFile = "lidarr.json";
    private const string ContentBlockerConfigFile = "contentblocker.json";
    private const string QueueCleanerConfigFile = "queuecleaner.json";
    private const string DownloadCleanerConfigFile = "downloadcleaner.json";
    private const string DownloadClientConfigFile = "downloadclient.json";
    private const string IgnoredDownloadsConfigFile = "ignoreddownloads.json";

    public ConfigManager(
        ILogger<ConfigManager> logger,
        JsonConfigurationProvider configProvider)
    {
        _logger = logger;
        _configProvider = configProvider;
    }

    // Generic configuration methods
    public Task<T?> GetConfigurationAsync<T>(string configFileName) where T : class, new()
    {
        return _configProvider.ReadConfigurationAsync<T>(configFileName);
    }

    public Task<bool> SaveConfigurationAsync<T>(string configFileName, T config) where T : class
    {
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
    public async Task<SonarrConfig?> GetSonarrConfigAsync()
    {
        return await _configProvider.ReadConfigurationAsync<SonarrConfig>(SonarrConfigFile);
    }

    public async Task<RadarrConfig?> GetRadarrConfigAsync()
    {
        return await _configProvider.ReadConfigurationAsync<RadarrConfig>(RadarrConfigFile);
    }

    public async Task<LidarrConfig?> GetLidarrConfigAsync()
    {
        return await _configProvider.ReadConfigurationAsync<LidarrConfig>(LidarrConfigFile);
    }

    public async Task<ContentBlockerConfig?> GetContentBlockerConfigAsync()
    {
        return await _configProvider.ReadConfigurationAsync<ContentBlockerConfig>(ContentBlockerConfigFile);
    }
    
    public ContentBlockerConfig? GetContentBlockerConfig()
    {
        return _configProvider.ReadConfiguration<ContentBlockerConfig>(ContentBlockerConfigFile);
    }

    public async Task<QueueCleanerConfig?> GetQueueCleanerConfigAsync()
    {
        return await _configProvider.ReadConfigurationAsync<QueueCleanerConfig>(QueueCleanerConfigFile);
    }

    public async Task<DownloadCleanerConfig?> GetDownloadCleanerConfigAsync()
    {
        return await _configProvider.ReadConfigurationAsync<DownloadCleanerConfig>(DownloadCleanerConfigFile);
    }

    public async Task<DownloadClientConfig?> GetDownloadClientConfigAsync()
    {
        return await _configProvider.ReadConfigurationAsync<DownloadClientConfig>(DownloadClientConfigFile);
    }

    public async Task<IgnoredDownloadsConfig?> GetIgnoredDownloadsConfigAsync()
    {
        return await _configProvider.ReadConfigurationAsync<IgnoredDownloadsConfig>(IgnoredDownloadsConfigFile);
    }

    public Task<bool> SaveSonarrConfigAsync(SonarrConfig config)
    {
        try
        {
            config.Validate();
            return _configProvider.WriteConfigurationAsync(SonarrConfigFile, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sonarr configuration validation failed");
            return Task.FromResult(false);
        }
    }

    public Task<bool> SaveRadarrConfigAsync(RadarrConfig config)
    {
        try
        {
            config.Validate();
            return _configProvider.WriteConfigurationAsync(RadarrConfigFile, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Radarr configuration validation failed");
            return Task.FromResult(false);
        }
    }

    public Task<bool> SaveLidarrConfigAsync(LidarrConfig config)
    {
        try
        {
            config.Validate();
            return _configProvider.WriteConfigurationAsync(LidarrConfigFile, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lidarr configuration validation failed");
            return Task.FromResult(false);
        }
    }

    public Task<bool> SaveContentBlockerConfigAsync(ContentBlockerConfig config)
    {
        try
        {
            config.Validate();
            return _configProvider.WriteConfigurationAsync(ContentBlockerConfigFile, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ContentBlocker configuration validation failed");
            return Task.FromResult(false);
        }
    }

    public Task<bool> SaveQueueCleanerConfigAsync(QueueCleanerConfig config)
    {
        try
        {
            config.Validate();
            return _configProvider.WriteConfigurationAsync(QueueCleanerConfigFile, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QueueCleaner configuration validation failed");
            return Task.FromResult(false);
        }
    }

    public Task<bool> SaveDownloadCleanerConfigAsync(DownloadCleanerConfig config)
    {
        try
        {
            config.Validate();
            return _configProvider.WriteConfigurationAsync(DownloadCleanerConfigFile, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DownloadCleaner configuration validation failed");
            return Task.FromResult(false);
        }
    }

    public Task<bool> SaveDownloadClientConfigAsync(DownloadClientConfig config)
    {
        try
        {
            config.Validate();
            return _configProvider.WriteConfigurationAsync(DownloadClientConfigFile, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DownloadClient configuration validation failed");
            return Task.FromResult(false);
        }
    }
    
    public async Task<IgnoredDownloadsConfig?> GetIgnoredDownloadsConfigAsync()
    {
        return await _configProvider.ReadConfigurationAsync<IgnoredDownloadsConfig>(IgnoredDownloadsConfigFile);
    }

    public Task<bool> SaveIgnoredDownloadsConfigAsync(IgnoredDownloadsConfig config)
    {
        try
        {
            return _configProvider.WriteConfigurationAsync(IgnoredDownloadsConfigFile, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IgnoredDownloads configuration save failed");
            return Task.FromResult(false);
        }
    }
    
    // Generic synchronous configuration methods
    public T? GetConfiguration<T>(string fileName) where T : class, new()
    {
        return _jsonConfigurationProvider.ReadConfiguration<T>(fileName);
    }
    
    public Common.Configuration.QueueCleaner.QueueCleanerConfig? GetQueueCleanerConfig()
    {
        return GetConfiguration<Common.Configuration.QueueCleaner.QueueCleanerConfig>("queue-cleaner.json");
    }
    
    public bool SaveConfiguration<T>(string configFileName, T config) where T : class
    {
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

        return _configProvider.WriteConfiguration(configFileName, config);
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
    public SonarrConfig? GetSonarrConfig()
    {
        return _configProvider.ReadConfiguration<SonarrConfig>(SonarrConfigFile);
    }
    
    public RadarrConfig? GetRadarrConfig()
    {
        return _configProvider.ReadConfiguration<RadarrConfig>(RadarrConfigFile);
    }
    
    public LidarrConfig? GetLidarrConfig()
    {
        return _configProvider.ReadConfiguration<LidarrConfig>(LidarrConfigFile);
    }
    
    public QueueCleanerConfig? GetQueueCleanerConfig()
    {
        return _configProvider.ReadConfiguration<QueueCleanerConfig>(QueueCleanerConfigFile);
    }
    
    public DownloadCleanerConfig? GetDownloadCleanerConfig()
    {
        return _configProvider.ReadConfiguration<DownloadCleanerConfig>(DownloadCleanerConfigFile);
    }
    
    public DownloadClientConfig? GetDownloadClientConfig()
    {
        return _configProvider.ReadConfiguration<DownloadClientConfig>(DownloadClientConfigFile);
    }
    
    public IgnoredDownloadsConfig? GetIgnoredDownloadsConfig()
    {
        return _configProvider.ReadConfiguration<IgnoredDownloadsConfig>(IgnoredDownloadsConfigFile);
    }
    
    public bool SaveSonarrConfig(SonarrConfig config)
    {
        try
        {
            config.Validate();
            return _configProvider.WriteConfiguration(SonarrConfigFile, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sonarr configuration validation failed");
            return false;
        }
    }
    
    public bool SaveRadarrConfig(RadarrConfig config)
    {
        try
        {
            config.Validate();
            return _configProvider.WriteConfiguration(RadarrConfigFile, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Radarr configuration validation failed");
            return false;
        }
    }
    
    public bool SaveLidarrConfig(LidarrConfig config)
    {
        try
        {
            config.Validate();
            return _configProvider.WriteConfiguration(LidarrConfigFile, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lidarr configuration validation failed");
            return false;
        }
    }
    
    public bool SaveContentBlockerConfig(ContentBlockerConfig config)
    {
        try
        {
            config.Validate();
            return _configProvider.WriteConfiguration(ContentBlockerConfigFile, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ContentBlocker configuration validation failed");
            return false;
        }
    }
    
    public bool SaveQueueCleanerConfig(QueueCleanerConfig config)
    {
        try
        {
            config.Validate();
            return _configProvider.WriteConfiguration(QueueCleanerConfigFile, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QueueCleaner configuration validation failed");
            return false;
        }
    }
    
    public bool SaveDownloadCleanerConfig(DownloadCleanerConfig config)
    {
        try
        {
            config.Validate();
            return _configProvider.WriteConfiguration(DownloadCleanerConfigFile, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DownloadCleaner configuration validation failed");
            return false;
        }
    }
    
    public bool SaveDownloadClientConfig(DownloadClientConfig config)
    {
        try
        {
            config.Validate();
            return _configProvider.WriteConfiguration(DownloadClientConfigFile, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DownloadClient configuration validation failed");
            return false;
        }
    }
    
    public bool SaveIgnoredDownloadsConfig(IgnoredDownloadsConfig config)
    {
        try
        {
            return _configProvider.WriteConfiguration(IgnoredDownloadsConfigFile, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IgnoredDownloads configuration save failed");
            return false;
        }
    }
}
