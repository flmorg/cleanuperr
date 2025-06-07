using System.Collections.Concurrent;
using Common.Helpers;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Configuration;

/// <summary>
/// Caching wrapper for IConfigurationProvider that minimizes disk access
/// and uses FileSystemWatcher to detect external changes
/// </summary>
public class CachedConfigurationProvider : IConfigurationProvider, IDisposable
{
    private readonly ILogger<CachedConfigurationProvider> _logger;
    private readonly IConfigurationProvider _baseProvider;
    private readonly string _configDirectory;
    private readonly ConcurrentDictionary<string, object> _configCache = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastModifiedTimes = new();
    private readonly FileSystemWatcher _fileWatcher;

    public CachedConfigurationProvider(
        ILogger<CachedConfigurationProvider> logger,
        JsonConfigurationProvider baseProvider
    )
    {
        _logger = logger;
        _baseProvider = baseProvider;
        _configDirectory = ConfigurationPathProvider.GetSettingsPath();
        
        // Ensure directory exists
        if (!Directory.Exists(_configDirectory))
        {
            Directory.CreateDirectory(_configDirectory);
            _logger.LogInformation("Created configuration directory: {directory}", _configDirectory);
        }

        // Set up file watcher
        _fileWatcher = new FileSystemWatcher(_configDirectory)
        {
            EnableRaisingEvents = true,
            IncludeSubdirectories = false,
            Filter = "*.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
        };

        _fileWatcher.Changed += OnFileChanged;
        _fileWatcher.Created += OnFileCreated;
        _fileWatcher.Deleted += OnFileDeleted;
        _fileWatcher.Renamed += OnFileRenamed;

        _logger.LogInformation("Initialized cached configuration provider for directory: {directory}", _configDirectory);
    }
    
    public bool FileExists(string fileName)
    {
        return _baseProvider.FileExists(fileName);
    }

    public T ReadConfiguration<T>(string fileName) where T : class, new()
    {
        var cacheKey = GetCacheKey<T>(fileName);
        
        // Try to get from cache first
        if (_configCache.TryGetValue(cacheKey, out var cachedValue) && cachedValue is T cachedConfig)
        {
            // Check if file has been modified since last cache
            if (!IsFileModifiedSinceLastRead(fileName))
            {
                _logger.LogTrace("Cache hit for configuration: {file}", fileName);
                return cachedConfig;
            }
            
            _logger.LogDebug("Cache invalidated due to file change: {file}", fileName);
        }

        // Read from provider and update cache
        var config = _baseProvider.ReadConfiguration<T>(fileName);
        
        // If no configuration exists, create a default one
        if (config == null)
        {
            config = new T();
            _logger.LogInformation("Created default configuration for: {file}", fileName);
        }
        
        // Update cache with either loaded or default config
        UpdateCache(cacheKey, fileName, config);
        
        return config;
    }

    public async Task<T> ReadConfigurationAsync<T>(string fileName) where T : class, new()
    {
        var cacheKey = GetCacheKey<T>(fileName);
        
        // Try to get from cache first
        if (_configCache.TryGetValue(cacheKey, out var cachedValue) && cachedValue is T cachedConfig)
        {
            // Check if file has been modified since last cache
            if (!IsFileModifiedSinceLastRead(fileName))
            {
                _logger.LogTrace("Cache hit for configuration: {file}", fileName);
                return cachedConfig;
            }
            
            _logger.LogDebug("Cache invalidated due to file change: {file}", fileName);
        }

        // Read from provider and update cache
        var config = await _baseProvider.ReadConfigurationAsync<T>(fileName);
        
        // If no configuration exists, create a default one
        if (config == null)
        {
            config = new T();
            _logger.LogInformation("Created default configuration for: {file}", fileName);
        }
        
        // Update cache with either loaded or default config
        UpdateCache(cacheKey, fileName, config);
        
        return config;
    }

    public bool WriteConfiguration<T>(string fileName, T configuration) where T : class
    {
        var result = _baseProvider.WriteConfiguration(fileName, configuration);
        if (result)
        {
            // Update cache immediately rather than waiting for file watcher
            var cacheKey = GetCacheKey<T>(fileName);
            UpdateCache(cacheKey, fileName, configuration);
        }
        
        return result;
    }

    public async Task<bool> WriteConfigurationAsync<T>(string fileName, T configuration) where T : class
    {
        var result = await _baseProvider.WriteConfigurationAsync(fileName, configuration);
        if (result)
        {
            // Update cache immediately rather than waiting for file watcher
            var cacheKey = GetCacheKey<T>(fileName);
            UpdateCache(cacheKey, fileName, configuration);
        }
        
        return result;
    }

    public bool UpdateConfigurationProperty<T>(string fileName, string propertyPath, T value)
    {
        var result = _baseProvider.UpdateConfigurationProperty(fileName, propertyPath, value);
        if (result)
        {
            // Invalidate the cache for this file
            InvalidateCache(fileName);
        }
        
        return result;
    }

    public async Task<bool> UpdateConfigurationPropertyAsync<T>(string fileName, string propertyPath, T value)
    {
        var result = await _baseProvider.UpdateConfigurationPropertyAsync(fileName, propertyPath, value);
        if (result)
        {
            // Invalidate the cache for this file
            InvalidateCache(fileName);
        }
        
        return result;
    }

    public bool MergeConfiguration<T>(string fileName, T newValues) where T : class
    {
        var result = _baseProvider.MergeConfiguration(fileName, newValues);
        if (result)
        {
            // Invalidate the cache for this file
            InvalidateCache(fileName);
        }
        
        return result;
    }

    public async Task<bool> MergeConfigurationAsync<T>(string fileName, T newValues) where T : class
    {
        var result = await _baseProvider.MergeConfigurationAsync(fileName, newValues);
        if (result)
        {
            // Invalidate the cache for this file
            InvalidateCache(fileName);
        }
        
        return result;
    }

    public bool DeleteConfiguration(string fileName)
    {
        var result = _baseProvider.DeleteConfiguration(fileName);
        if (result)
        {
            // Remove from cache
            InvalidateCache(fileName);
        }
        
        return result;
    }

    public async Task<bool> DeleteConfigurationAsync(string fileName)
    {
        var result = await _baseProvider.DeleteConfigurationAsync(fileName);
        if (result)
        {
            // Remove from cache
            InvalidateCache(fileName);
        }
        
        return result;
    }

    public IEnumerable<string> ListConfigurationFiles()
    {
        return _baseProvider.ListConfigurationFiles();
    }

    // Private helper methods
    private static string GetCacheKey<T>(string fileName)
    {
        return $"{fileName}_{typeof(T).FullName}";
    }
    
    private void UpdateCache<T>(string cacheKey, string fileName, T value) where T : class
    {
        _configCache[cacheKey] = value;
        _lastModifiedTimes[fileName] = GetLastWriteTime(fileName);
        _logger.LogDebug("Updated cache for: {file}", fileName);
    }
    
    private void InvalidateCache(string fileName)
    {
        // Find and remove all cache entries that start with this filename
        var keysToRemove = _configCache.Keys
            .Where(k => k.StartsWith($"{fileName}_"))
            .ToList();
            
        foreach (var key in keysToRemove)
        {
            _configCache.TryRemove(key, out _);
        }
        
        _lastModifiedTimes.TryRemove(fileName, out _);
        _logger.LogDebug("Invalidated cache for: {file}", fileName);
    }
    
    private bool IsFileModifiedSinceLastRead(string fileName)
    {
        if (!_lastModifiedTimes.TryGetValue(fileName, out var lastReadTime))
        {
            return true; // Not in cache, so treat as modified
        }
        
        var lastWriteTime = GetLastWriteTime(fileName);
        return lastWriteTime > lastReadTime;
    }
    
    private DateTime GetLastWriteTime(string fileName)
    {
        var fullPath = Path.Combine(_configDirectory, fileName);
        if (!File.Exists(fullPath))
        {
            return DateTime.MinValue;
        }
        
        return File.GetLastWriteTimeUtc(fullPath);
    }
    
    // File watcher event handlers
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var fileName = Path.GetFileName(e.FullPath);
        _logger.LogInformation("Configuration file changed: {file}", fileName);
        InvalidateCache(fileName);
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        var fileName = Path.GetFileName(e.FullPath);
        _logger.LogInformation("Configuration file created: {file}", fileName);
        InvalidateCache(fileName);
    }
    
    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        var fileName = Path.GetFileName(e.FullPath);
        _logger.LogInformation("Configuration file deleted: {file}", fileName);
        InvalidateCache(fileName);
    }
    
    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        var oldFileName = Path.GetFileName(e.OldFullPath);
        var newFileName = Path.GetFileName(e.FullPath);
        
        _logger.LogInformation("Configuration file renamed from {oldFile} to {newFile}", oldFileName, newFileName);
        
        InvalidateCache(oldFileName);
    }
    
    // IDisposable implementation
    public void Dispose()
    {
        _fileWatcher.Changed -= OnFileChanged;
        _fileWatcher.Created -= OnFileCreated;
        _fileWatcher.Deleted -= OnFileDeleted;
        _fileWatcher.Renamed -= OnFileRenamed;
        _fileWatcher.Dispose();
        
        _logger.LogInformation("Disposed cached configuration provider");
    }
}
