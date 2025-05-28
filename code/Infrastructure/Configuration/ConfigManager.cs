using Common.Configuration;
using Common.Configuration.Arr;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.General;
using Common.Configuration.IgnoredDownloads;
using Common.Configuration.Notification;
using Common.Configuration.QueueCleaner;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Configuration;

public class ConfigManager : IConfigManager
{
    private readonly ILogger<ConfigManager> _logger;
    private readonly IConfigurationProvider _configProvider;

    private readonly Dictionary<Type, string> _settingsPaths;

    public ConfigManager(
        ILogger<ConfigManager> logger,
        IConfigurationProvider configProvider)
    {
        _logger = logger;
        _configProvider = configProvider;
        
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
    
    public Task<T> GetConfigurationAsync<T>() where T : class, new()
    {
        return _configProvider.ReadConfigurationAsync<T>(_settingsPaths[typeof(T)]);
    }
    
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

    public T GetConfiguration<T>() where T : class, new()
    {
        return _configProvider.ReadConfiguration<T>(_settingsPaths[typeof(T)]);
    }
    
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
}
