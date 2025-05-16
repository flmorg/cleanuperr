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
        _ = await _configManager.GetContentBlockerConfigAsync();
    }
    
    private async Task EnsureQueueCleanerConfigAsync()
    {
        _ = await _configManager.GetQueueCleanerConfigAsync();
    }
    
    private async Task EnsureDownloadCleanerConfigAsync()
    {
        _ = await _configManager.GetDownloadCleanerConfigAsync();
    }
    
    private async Task EnsureDownloadClientConfigAsync()
    {
        _ = await _configManager.GetDownloadClientConfigAsync();
    }
    
    private async Task EnsureSonarrConfigAsync()
    {
        _ = await _configManager.GetSonarrConfigAsync();
    }
    
    private async Task EnsureRadarrConfigAsync()
    {
        _ = await _configManager.GetRadarrConfigAsync();
    }
    
    private async Task EnsureLidarrConfigAsync()
    {
        _ = await _configManager.GetLidarrConfigAsync();
    }
    
    private async Task EnsureIgnoredDownloadsConfigAsync()
    {
        _ = await _configManager.GetIgnoredDownloadsConfigAsync();
    }
}
