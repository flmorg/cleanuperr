using Common.Configuration.ContentBlocker;
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

        if (config is null)
        {
            await _configManager.SaveContentBlockerConfigAsync(new());
        }
    }
    
    private async Task EnsureQueueCleanerConfigAsync()
    {
        var config = await _configManager.GetQueueCleanerConfigAsync();
        
        if (config is null)
        {
            await _configManager.SaveQueueCleanerConfigAsync(new());
        }
    }
    
    private async Task EnsureDownloadCleanerConfigAsync()
    {
        var config = await _configManager.GetDownloadCleanerConfigAsync();
        
        if (config is null)
        {
            await _configManager.SaveDownloadCleanerConfigAsync(new());
        }
    }
    
    private async Task EnsureDownloadClientConfigAsync()
    {
        var config = await _configManager.GetDownloadClientConfigAsync();
        
        if (config is null)
        {
            await _configManager.SaveDownloadClientConfigAsync(new());
        }
    }
    
    private async Task EnsureSonarrConfigAsync()
    {
        var config = await _configManager.GetSonarrConfigAsync();
        
        if (config is null)
        {
            await _configManager.SaveSonarrConfigAsync(new());
        }
    }
    
    private async Task EnsureRadarrConfigAsync()
    {
        var config = await _configManager.GetRadarrConfigAsync();
        
        if (config is null)
        {
            await _configManager.SaveRadarrConfigAsync(new());
        }
    }
    
    private async Task EnsureLidarrConfigAsync()
    {
        var config = await _configManager.GetLidarrConfigAsync();
        
        if (config is null)
        {
            await _configManager.SaveLidarrConfigAsync(new());
        }
    }
    
    private async Task EnsureIgnoredDownloadsConfigAsync()
    {
        var config = await _configManager.GetIgnoredDownloadsConfigAsync();
        
        if (config is null)
        {
            await _configManager.SaveIgnoredDownloadsConfigAsync(new());
        }
    }
}
