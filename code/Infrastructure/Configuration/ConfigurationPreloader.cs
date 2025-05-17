using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Configuration;

/// <summary>
/// Preloads all configurations at application startup to ensure they're cached
/// and ready for fast access
/// </summary>
public class ConfigurationPreloader : IHostedService
{
    private readonly IConfigManager _configManager;
    private readonly ILogger<ConfigurationPreloader> _logger;
    
    public ConfigurationPreloader(IConfigManager configManager, ILogger<ConfigurationPreloader> logger)
    {
        _configManager = configManager;
        _logger = logger;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Preloading all configurations...");
        
        try
        {
            // Load all configurations in parallel
            await Task.WhenAll(
                LoadConfigAsync("General config", _configManager.GetGeneralConfigAsync),
                LoadConfigAsync("Sonarr config", _configManager.GetSonarrConfigAsync),
                LoadConfigAsync("Radarr config", _configManager.GetRadarrConfigAsync),
                LoadConfigAsync("Lidarr config", _configManager.GetLidarrConfigAsync),
                LoadConfigAsync("Content blocker config", _configManager.GetContentBlockerConfigAsync),
                LoadConfigAsync("Queue cleaner config", _configManager.GetQueueCleanerConfigAsync),
                LoadConfigAsync("Download cleaner config", _configManager.GetDownloadCleanerConfigAsync),
                LoadConfigAsync("Download client config", _configManager.GetDownloadClientConfigAsync),
                LoadConfigAsync("Ignored downloads config", _configManager.GetIgnoredDownloadsConfigAsync),
                LoadConfigAsync("Notifications config", _configManager.GetNotificationsConfigAsync)
            );
            
            _logger.LogInformation("All configurations preloaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preloading configurations");
        }
    }
    
    private async Task LoadConfigAsync<T>(string configName, Func<Task<T>> loadFunction) where T : class
    {
        try
        {
            var config = await loadFunction();
            _logger.LogDebug("Preloaded {configName}", configName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preload {configName}", configName);
        }
    }
    
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
