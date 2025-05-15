using Common.Configuration;
using Common.Configuration.Arr;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.IgnoredDownloads;
using Common.Configuration.QueueCleaner;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Configuration;

/// <summary>
/// Provides configuration management for various components with thread-safe file access.
/// </summary>
public interface IConfigManager
{
    // Configuration files
    Task<T?> GetConfigurationAsync<T>(string configFileName) where T : class, new();
    Task<bool> SaveConfigurationAsync<T>(string configFileName, T config) where T : class;
    Task<bool> UpdateConfigurationPropertyAsync<T>(string configFileName, string propertyPath, T value);
    Task<bool> MergeConfigurationAsync<T>(string configFileName, T newValues) where T : class;
    Task<bool> DeleteConfigurationAsync(string configFileName);
    IEnumerable<string> ListConfigurationFiles();

    // Specific configuration types (for common configs)
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
}

public class ConfigManager : IConfigManager
{
    private readonly ILogger<ConfigManager> _logger;
    private readonly JsonConfigurationProvider _configProvider;
    private readonly IOptionsMonitor<SonarrConfig> _sonarrConfig;
    private readonly IOptionsMonitor<RadarrConfig> _radarrConfig;
    private readonly IOptionsMonitor<LidarrConfig> _lidarrConfig;
    private readonly IOptionsMonitor<ContentBlockerConfig> _contentBlockerConfig;
    private readonly IOptionsMonitor<QueueCleanerConfig> _queueCleanerConfig;
    private readonly IOptionsMonitor<DownloadCleanerConfig> _downloadCleanerConfig;
    private readonly IOptionsMonitor<DownloadClientConfig> _downloadClientConfig;
    private readonly IOptionsMonitor<IgnoredDownloadsConfig> _ignoredDownloadsConfig;

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
        JsonConfigurationProvider configProvider,
        IOptionsMonitor<SonarrConfig> sonarrConfig,
        IOptionsMonitor<RadarrConfig> radarrConfig,
        IOptionsMonitor<LidarrConfig> lidarrConfig,
        IOptionsMonitor<ContentBlockerConfig> contentBlockerConfig,
        IOptionsMonitor<QueueCleanerConfig> queueCleanerConfig,
        IOptionsMonitor<DownloadCleanerConfig> downloadCleanerConfig,
        IOptionsMonitor<DownloadClientConfig> downloadClientConfig,
        IOptionsMonitor<IgnoredDownloadsConfig> ignoredDownloadsConfig)
    {
        _logger = logger;
        _configProvider = configProvider;
        _sonarrConfig = sonarrConfig;
        _radarrConfig = radarrConfig;
        _lidarrConfig = lidarrConfig;
        _contentBlockerConfig = contentBlockerConfig;
        _queueCleanerConfig = queueCleanerConfig;
        _downloadCleanerConfig = downloadCleanerConfig;
        _downloadClientConfig = downloadClientConfig;
        _ignoredDownloadsConfig = ignoredDownloadsConfig;
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
        var config = await _configProvider.ReadConfigurationAsync<SonarrConfig>(SonarrConfigFile);
        return config ?? _sonarrConfig.CurrentValue;
    }

    public async Task<RadarrConfig?> GetRadarrConfigAsync()
    {
        var config = await _configProvider.ReadConfigurationAsync<RadarrConfig>(RadarrConfigFile);
        return config ?? _radarrConfig.CurrentValue;
    }

    public async Task<LidarrConfig?> GetLidarrConfigAsync()
    {
        var config = await _configProvider.ReadConfigurationAsync<LidarrConfig>(LidarrConfigFile);
        return config ?? _lidarrConfig.CurrentValue;
    }

    public async Task<ContentBlockerConfig?> GetContentBlockerConfigAsync()
    {
        var config = await _configProvider.ReadConfigurationAsync<ContentBlockerConfig>(ContentBlockerConfigFile);
        return config ?? _contentBlockerConfig.CurrentValue;
    }

    public async Task<QueueCleanerConfig?> GetQueueCleanerConfigAsync()
    {
        var config = await _configProvider.ReadConfigurationAsync<QueueCleanerConfig>(QueueCleanerConfigFile);
        return config ?? _queueCleanerConfig.CurrentValue;
    }

    public async Task<DownloadCleanerConfig?> GetDownloadCleanerConfigAsync()
    {
        var config = await _configProvider.ReadConfigurationAsync<DownloadCleanerConfig>(DownloadCleanerConfigFile);
        return config ?? _downloadCleanerConfig.CurrentValue;
    }

    public async Task<DownloadClientConfig?> GetDownloadClientConfigAsync()
    {
        var config = await _configProvider.ReadConfigurationAsync<DownloadClientConfig>(DownloadClientConfigFile);
        return config ?? _downloadClientConfig.CurrentValue;
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
        var config = await _configProvider.ReadConfigurationAsync<IgnoredDownloadsConfig>(IgnoredDownloadsConfigFile);
        return config ?? _ignoredDownloadsConfig.CurrentValue;
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
}
