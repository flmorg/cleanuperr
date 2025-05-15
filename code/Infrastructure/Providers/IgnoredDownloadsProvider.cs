using Common.Configuration;
using Infrastructure.Configuration;
using Infrastructure.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers;

public sealed class IgnoredDownloadsProvider<T>
    where T : IIgnoredDownloadsConfig
{
    private readonly ILogger<IgnoredDownloadsProvider<T>> _logger;
    private IIgnoredDownloadsConfig _config;
    private readonly IConfigurationManager _configManager;
    private readonly IMemoryCache _cache;
    private DateTime _lastModified = DateTime.MinValue;
    private readonly string _configType;

    public IgnoredDownloadsProvider(ILogger<IgnoredDownloadsProvider<T>> logger, IConfigurationManager configManager, IMemoryCache cache)
    {
        _logger = logger;
        _configManager = configManager;
        _cache = cache;
        _configType = typeof(T).Name;
        
        // Initialize configuration
        InitializeConfig().Wait();
    }
    
    private async Task InitializeConfig()
    {
        // Get the configuration based on the type
        if (typeof(T).Name.Contains("ContentBlocker"))
        {
            var config = await _configManager.GetContentBlockerConfigAsync();
            _config = config ?? new T();
        }
        else if (typeof(T).Name.Contains("QueueCleaner"))
        {
            var config = await _configManager.GetQueueCleanerConfigAsync();
            _config = config ?? new T();
        }
        else if (typeof(T).Name.Contains("DownloadCleaner"))
        {
            var config = await _configManager.GetDownloadCleanerConfigAsync();
            _config = config ?? new T();
        }
        else
        {
            _config = new T();
        }

        if (string.IsNullOrEmpty(_config.IgnoredDownloadsPath))
        {
            return;
        }

        if (!File.Exists(_config.IgnoredDownloadsPath))
        {
            _logger.LogWarning("Ignored downloads file not found: {path}", _config.IgnoredDownloadsPath);
        }
    }

    public async Task<IReadOnlyList<string>> GetIgnoredDownloads()
    {
        // Refresh the configuration before checking ignored downloads
        await InitializeConfig();
        
        if (string.IsNullOrEmpty(_config.IgnoredDownloadsPath))
        {
            return Array.Empty<string>();
        }

        FileInfo fileInfo = new(_config.IgnoredDownloadsPath);
        
        if (fileInfo.LastWriteTime > _lastModified ||
            !_cache.TryGetValue(CacheKeys.IgnoredDownloads(_configType), out IReadOnlyList<string>? ignoredDownloads) ||
            ignoredDownloads is null)
        {
            _lastModified = fileInfo.LastWriteTime;

            return await LoadFile();
        }
        
        return ignoredDownloads;
    }

    private async Task<IReadOnlyList<string>> LoadFile()
    {
        try
        {
            if (string.IsNullOrEmpty(_config.IgnoredDownloadsPath))
            {
                return Array.Empty<string>();
            }

            string[] ignoredDownloads = (await File.ReadAllLinesAsync(_config.IgnoredDownloadsPath))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            _cache.Set(CacheKeys.IgnoredDownloads(typeof(T).Name), ignoredDownloads);

            _logger.LogInformation("ignored downloads reloaded");

            return ignoredDownloads;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "error while reading ignored downloads file | {file}", _config.IgnoredDownloadsPath);
        }

        return Array.Empty<string>();
    }
}