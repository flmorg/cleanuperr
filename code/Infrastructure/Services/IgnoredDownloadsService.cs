using Common.Configuration.IgnoredDownloads;
using Infrastructure.Configuration;
using Infrastructure.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing ignored downloads
/// </summary>
public interface IIgnoredDownloadsService
{
    /// <summary>
    /// Gets the list of ignored download IDs
    /// </summary>
    Task<IReadOnlyList<string>> GetIgnoredDownloadsAsync();
    
    /// <summary>
    /// Adds a download ID to the ignored list
    /// </summary>
    Task<bool> AddIgnoredDownloadAsync(string downloadId);
    
    /// <summary>
    /// Removes a download ID from the ignored list
    /// </summary>
    Task<bool> RemoveIgnoredDownloadAsync(string downloadId);
    
    /// <summary>
    /// Clears all ignored downloads
    /// </summary>
    Task<bool> ClearIgnoredDownloadsAsync();
}

public class IgnoredDownloadsService : IIgnoredDownloadsService
{
    private readonly ILogger<IgnoredDownloadsService> _logger;
    private readonly IConfigManager _configManager;
    
    public IgnoredDownloadsService(
        ILogger<IgnoredDownloadsService> logger,
        IConfigManager configManager
    )
    {
        _logger = logger;
        _configManager = configManager;
    }
    
    public async Task<IReadOnlyList<string>> GetIgnoredDownloadsAsync()
    {
        var config = await _configManager.GetIgnoredDownloadsConfigAsync();
        if (config == null)
        {
            return Array.Empty<string>();
        }
        
        return config.IgnoredDownloads;
    }
    
    public async Task<bool> AddIgnoredDownloadAsync(string downloadId)
    {
        if (string.IsNullOrWhiteSpace(downloadId))
        {
            return false;
        }
        
        var config = await _configManager.GetIgnoredDownloadsConfigAsync();
        if (config == null)
        {
            config = new IgnoredDownloadsConfig
            {
                IgnoredDownloads = [downloadId]
            };
        }
        else if (!config.IgnoredDownloads.Contains(downloadId, StringComparer.OrdinalIgnoreCase))
        {
            var updatedList = config.IgnoredDownloads;
            updatedList.Add(downloadId);
            config = new IgnoredDownloadsConfig
            {
                IgnoredDownloads = updatedList
            };
        }
        else
        {
            // Already in the list
            return true;
        }
        
        var result = await _configManager.SaveIgnoredDownloadsConfigAsync(config);
        if (result)
        {
            _logger.LogInformation("Added download ID to ignored list: {downloadId}", downloadId);
        }
        
        return result;
    }
    
    public async Task<bool> RemoveIgnoredDownloadAsync(string downloadId)
    {
        if (string.IsNullOrWhiteSpace(downloadId))
        {
            return false;
        }
        
        var config = await _configManager.GetIgnoredDownloadsConfigAsync();
        if (config == null || config.IgnoredDownloads.Count == 0)
        {
            return true; // Nothing to remove
        }
        
        var updatedList = config.IgnoredDownloads
            .Where(id => !string.Equals(id, downloadId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        if (updatedList.Count == config.IgnoredDownloads.Count)
        {
            return true;
        }
        
        var newConfig = new IgnoredDownloadsConfig
        {
            IgnoredDownloads = updatedList
        };
        
        var result = await _configManager.SaveIgnoredDownloadsConfigAsync(newConfig);
        if (result)
        {
            _logger.LogInformation("Removed download ID from ignored list: {downloadId}", downloadId);
        }
        
        return result;
    }
    
    public async Task<bool> ClearIgnoredDownloadsAsync()
    {
        var config = new IgnoredDownloadsConfig
        {
            IgnoredDownloads = new List<string>()
        };
        
        var result = await _configManager.SaveIgnoredDownloadsConfigAsync(config);
        
        if (result)
        {
            // Update cache
            _logger.LogInformation("Cleared all ignored downloads");
        }
        
        return result;
    }
}
