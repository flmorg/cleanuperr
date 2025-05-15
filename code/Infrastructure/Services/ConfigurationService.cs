using Common.Configuration;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.IgnoredDownloads;
using Common.Configuration.QueueCleaner;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public interface IConfigurationService
{
    Task<bool> UpdateConfigurationAsync<T>(string sectionName, T configSection);
    Task<T?> GetConfigurationAsync<T>(string sectionName);
    Task<SonarrConfig?> GetSonarrConfigAsync();
    Task<RadarrConfig?> GetRadarrConfigAsync();
    Task<LidarrConfig?> GetLidarrConfigAsync();
    Task<ContentBlockerConfig?> GetContentBlockerConfigAsync();
    Task<QueueCleanerConfig?> GetQueueCleanerConfigAsync();
    Task<DownloadCleanerConfig?> GetDownloadCleanerConfigAsync();
    Task<DownloadClientConfig?> GetDownloadClientConfigAsync();
    Task<IgnoredDownloadsConfig?> GetIgnoredDownloadsConfigAsync();
    Task<bool> UpdateSonarrConfigAsync(SonarrConfig config);
    Task<bool> UpdateRadarrConfigAsync(RadarrConfig config);
    Task<bool> UpdateLidarrConfigAsync(LidarrConfig config);
    Task<bool> UpdateContentBlockerConfigAsync(ContentBlockerConfig config);
    Task<bool> UpdateQueueCleanerConfigAsync(QueueCleanerConfig config);
    Task<bool> UpdateDownloadCleanerConfigAsync(DownloadCleanerConfig config);
    Task<bool> UpdateDownloadClientConfigAsync(DownloadClientConfig config);
    Task<bool> UpdateIgnoredDownloadsConfigAsync(IgnoredDownloadsConfig config);
}

public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IConfigurationManager _configManager;

    public ConfigurationService(
        ILogger<ConfigurationService> logger,
        IConfigurationManager configManager)
    {
        _logger = logger;
        _configManager = configManager;
    }

    public async Task<bool> UpdateConfigurationAsync<T>(string sectionName, T configSection)
    {
        try
        {
            // This is just a placeholder method for backward compatibility
            // The actual implementation depends on the specific config type
            _logger.LogWarning("Using deprecated UpdateConfigurationAsync method with section name '{section}'.", sectionName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration section {section}", sectionName);
            return false;
        }
    }

    public async Task<T?> GetConfigurationAsync<T>(string sectionName)
    {
        try
        {
            // This is just a placeholder method for backward compatibility
            // The actual implementation depends on the specific config type
            _logger.LogWarning("Using deprecated GetConfigurationAsync method with section name '{section}'.", sectionName);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration section {section}", sectionName);
            return default;
        }
    }

    // Specific configuration getters
    public async Task<SonarrConfig?> GetSonarrConfigAsync() => await _configManager.GetSonarrConfigAsync();
    public async Task<RadarrConfig?> GetRadarrConfigAsync() => await _configManager.GetRadarrConfigAsync();
    public async Task<LidarrConfig?> GetLidarrConfigAsync() => await _configManager.GetLidarrConfigAsync();
    public async Task<ContentBlockerConfig?> GetContentBlockerConfigAsync() => await _configManager.GetContentBlockerConfigAsync();
    public async Task<QueueCleanerConfig?> GetQueueCleanerConfigAsync() => await _configManager.GetQueueCleanerConfigAsync();
    public async Task<DownloadCleanerConfig?> GetDownloadCleanerConfigAsync() => await _configManager.GetDownloadCleanerConfigAsync();
    public async Task<DownloadClientConfig?> GetDownloadClientConfigAsync() => await _configManager.GetDownloadClientConfigAsync();
    public async Task<IgnoredDownloadsConfig?> GetIgnoredDownloadsConfigAsync() => await _configManager.GetIgnoredDownloadsConfigAsync();
    
    // Specific configuration setters
    public async Task<bool> UpdateSonarrConfigAsync(SonarrConfig config) => await _configManager.SaveSonarrConfigAsync(config);
    public async Task<bool> UpdateRadarrConfigAsync(RadarrConfig config) => await _configManager.SaveRadarrConfigAsync(config);
    public async Task<bool> UpdateLidarrConfigAsync(LidarrConfig config) => await _configManager.SaveLidarrConfigAsync(config);
    public async Task<bool> UpdateContentBlockerConfigAsync(ContentBlockerConfig config) => await _configManager.SaveContentBlockerConfigAsync(config);
    public async Task<bool> UpdateQueueCleanerConfigAsync(QueueCleanerConfig config) => await _configManager.SaveQueueCleanerConfigAsync(config);
    public async Task<bool> UpdateDownloadCleanerConfigAsync(DownloadCleanerConfig config) => await _configManager.SaveDownloadCleanerConfigAsync(config);
    public async Task<bool> UpdateDownloadClientConfigAsync(DownloadClientConfig config) => await _configManager.SaveDownloadClientConfigAsync(config);
    public async Task<bool> UpdateIgnoredDownloadsConfigAsync(IgnoredDownloadsConfig config) => await _configManager.SaveIgnoredDownloadsConfigAsync(config);
}
