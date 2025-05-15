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
    private readonly IConfigurationManager _configManager;
    private readonly IMemoryCache _cache;
    private const string IgnoredDownloadsCacheKey = "IgnoredDownloads";
    
    public IgnoredDownloadsService(
        ILogger<IgnoredDownloadsService> logger,
        IConfigurationManager configManager,
        IMemoryCache cache)
    {
        _logger = logger;
        _configManager = configManager;
        _cache = cache;
    }
    
    public async Task<IReadOnlyList<string>> GetIgnoredDownloadsAsync()
    {
        // Try to get from cache first
        if (_cache.TryGetValue(IgnoredDownloadsCacheKey, out IReadOnlyList<string>? cachedList) && 
            cachedList != null)
        {
            return cachedList;
        }
        
        // Not in cache, load from config
        var config = await _configManager.GetIgnoredDownloadsConfigAsync();
        if (config == null)
        {
            return Array.Empty<string>();
        }
        
        // Store in cache for quick access (5 minute expiration)
        var ignoredDownloads = config.IgnoredDownloads.ToList();
        _cache.Set(IgnoredDownloadsCacheKey, ignoredDownloads, TimeSpan.FromMinutes(5));
        
        return ignoredDownloads;
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
                IgnoredDownloads = new List<string> { downloadId }
            };
        }
        else if (!config.IgnoredDownloads.Contains(downloadId, StringComparer.OrdinalIgnoreCase))
        {
            var updatedList = config.IgnoredDownloads.ToList();
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
            // Update cache
            _cache.Remove(IgnoredDownloadsCacheKey);
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
            return true; // Item wasn't in the list
        }
        
        var newConfig = new IgnoredDownloadsConfig
        {
            IgnoredDownloads = updatedList
        };
        
        var result = await _configManager.SaveIgnoredDownloadsConfigAsync(newConfig);
        if (result)
        {
            // Update cache
            _cache.Remove(IgnoredDownloadsCacheKey);
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
            _cache.Remove(IgnoredDownloadsCacheKey);
            _logger.LogInformation("Cleared all ignored downloads");
        }
        
        return result;
    }
}
