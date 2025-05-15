using Common.Configuration.Arr;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.IgnoredDownloads;
using Common.Configuration.QueueCleaner;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Configuration;

/// <summary>
/// Initializes default configuration files if they don't exist
/// </summary>
public class ConfigInitializer
{
    private readonly IConfigManager _configManager;
    private readonly ILogger<ConfigInitializer> _logger;

    public ConfigInitializer(IConfigManager configManager, ILogger<ConfigInitializer> logger)
    {
        _configManager = configManager;
        _logger = logger;
    }

    /// <summary>
    /// Ensures all necessary configuration files exist
    /// </summary>
    public async Task EnsureConfigFilesExistAsync()
    {
        _logger.LogInformation("Initializing configuration files...");
        
        await EnsureContentBlockerConfigAsync();
        await EnsureQueueCleanerConfigAsync();
        await EnsureDownloadCleanerConfigAsync();
        await EnsureDownloadClientConfigAsync();
        await EnsureSonarrConfigAsync();
        await EnsureRadarrConfigAsync();
        await EnsureLidarrConfigAsync();
        await EnsureIgnoredDownloadsConfigAsync();
        
        _logger.LogInformation("Configuration files initialized");
    }
    
    private async Task EnsureContentBlockerConfigAsync()
    {
        var config = await _configManager.GetContentBlockerConfigAsync();
        if (config == null)
        {
            _logger.LogInformation("Creating default ContentBlocker configuration");
            var defaultConfig = new ContentBlockerConfig
            {
                Enabled = false,
                CronExpression = "0 */30 * ? * *", // Every 30 minutes
                DeletePrivate = false,
                IgnorePrivate = true,
                Sonarr = new BlocklistSettings
                {
                    Enabled = false,
                    Type = BlocklistType.Blacklist,
                    Path = ""
                },
                Radarr = new BlocklistSettings
                {
                    Enabled = false,
                    Type = BlocklistType.Blacklist,
                    Path = ""
                },
                Lidarr = new BlocklistSettings
                {
                    Enabled = false,
                    Type = BlocklistType.Blacklist,
                    Path = ""
                }
            };
            await _configManager.SaveContentBlockerConfigAsync(defaultConfig);
        }
    }
    
    private async Task EnsureQueueCleanerConfigAsync()
    {
        var config = await _configManager.GetQueueCleanerConfigAsync();
        if (config == null)
        {
            _logger.LogInformation("Creating default QueueCleaner configuration");
            var defaultConfig = new QueueCleanerConfig
            {
                Enabled = false,
                CronExpression = "0 */15 * ? * *", // Every 15 minutes
                RunSequentially = false,
                ImportFailedMaxStrikes = 3,
                StalledMaxStrikes = 3,
                SlowMaxStrikes = 3
            };
            await _configManager.SaveQueueCleanerConfigAsync(defaultConfig);
        }
    }
    
    private async Task EnsureDownloadCleanerConfigAsync()
    {
        var config = await _configManager.GetDownloadCleanerConfigAsync();
        if (config == null)
        {
            _logger.LogInformation("Creating default DownloadCleaner configuration");
            var defaultConfig = new DownloadCleanerConfig
            {
                Enabled = false,
                CronExpression = "0 */20 * ? * *", // Every 20 minutes
                DeletePrivate = false,
                UnlinkedTargetCategory = "cleanuperr-unlinked",
                UnlinkedUseTag = false
            };
            await _configManager.SaveDownloadCleanerConfigAsync(defaultConfig);
        }
    }
    
    private async Task EnsureDownloadClientConfigAsync()
    {
        var config = await _configManager.GetDownloadClientConfigAsync();
        if (config == null)
        {
            _logger.LogInformation("Creating default DownloadClient configuration");
            var defaultConfig = new DownloadClientConfig
            {
                Clients = []
            };
            await _configManager.SaveDownloadClientConfigAsync(defaultConfig);
        }
    }
    
    private async Task EnsureSonarrConfigAsync()
    {
        var config = await _configManager.GetSonarrConfigAsync();
        if (config == null)
        {
            _logger.LogInformation("Creating default Sonarr configuration");
            var defaultConfig = new SonarrConfig
            {
                Enabled = false,
                ImportFailedMaxStrikes = 3,
                SearchType = SonarrSearchType.Episode,
                Instances = new List<ArrInstance>()
            };
            await _configManager.SaveSonarrConfigAsync(defaultConfig);
        }
    }
    
    private async Task EnsureRadarrConfigAsync()
    {
        var config = await _configManager.GetRadarrConfigAsync();
        if (config == null)
        {
            _logger.LogInformation("Creating default Radarr configuration");
            var defaultConfig = new RadarrConfig
            {
                Enabled = false,
                ImportFailedMaxStrikes = 3,
                Instances = new List<ArrInstance>()
            };
            await _configManager.SaveRadarrConfigAsync(defaultConfig);
        }
    }
    
    private async Task EnsureLidarrConfigAsync()
    {
        var config = await _configManager.GetLidarrConfigAsync();
        if (config == null)
        {
            _logger.LogInformation("Creating default Lidarr configuration");
            var defaultConfig = new LidarrConfig
            {
                Enabled = false,
                ImportFailedMaxStrikes = 3,
                Instances = new List<ArrInstance>()
            };
            await _configManager.SaveLidarrConfigAsync(defaultConfig);
        }
    }
    
    private async Task EnsureIgnoredDownloadsConfigAsync()
    {
        var config = await _configManager.GetIgnoredDownloadsConfigAsync();
        if (config == null)
        {
            _logger.LogInformation("Creating default IgnoredDownloads configuration");
            var defaultConfig = new IgnoredDownloadsConfig
            {
                IgnoredDownloads = new List<string>()
            };
            await _configManager.SaveIgnoredDownloadsConfigAsync(defaultConfig);
        }
    }
}
