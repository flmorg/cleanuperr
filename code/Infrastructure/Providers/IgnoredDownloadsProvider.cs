using Common.Configuration;
using Infrastructure.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Providers;

public sealed class IgnoredDownloadsProvider<T> : IDisposable
    where T : IIgnoredDownloadsConfig
{
    private readonly ILogger<IgnoredDownloadsProvider<T>> _logger;
    private IIgnoredDownloadsConfig _config;
    private readonly IMemoryCache _cache;
    private DateTime _lastModified = DateTime.MinValue;
    private readonly FileSystemWatcher _watcher;

    public IgnoredDownloadsProvider(ILogger<IgnoredDownloadsProvider<T>> logger, IOptionsMonitor<T> config, IMemoryCache cache)
    {
        _config = config.CurrentValue;
        config.OnChange((newValue) => _config = newValue);
        _logger = logger;
        _cache = cache;

        if (string.IsNullOrEmpty(_config.IgnoredDownloadsPath))
        {
            return;
        }

        if (!File.Exists(_config.IgnoredDownloadsPath))
        {
            throw new FileNotFoundException("file not found", _config.IgnoredDownloadsPath);
        }
        
        _watcher = new FileSystemWatcher(Path.GetDirectoryName(_config.IgnoredDownloadsPath)!)
        {
            Filter = Path.GetFileName(_config.IgnoredDownloadsPath),
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        _watcher.Changed += async (s, e) => await LoadFile();
        _watcher.EnableRaisingEvents = true;
    }
    
    public async Task<IReadOnlyList<string>> GetIgnoredDownloads()
    {
        if (string.IsNullOrEmpty(_config.IgnoredDownloadsPath))
        {
            return Array.Empty<string>();
        }

        if (!_cache.TryGetValue(CacheKeys.IgnoredDownloads(typeof(T).Name), out IReadOnlyList<string>? ignoredDownloads))
        {
            return await LoadFile();
        }

        return ignoredDownloads ?? Array.Empty<string>();
    }

    private async Task<IReadOnlyList<string>> LoadFile()
    {
        try
        {
            if (string.IsNullOrEmpty(_config.IgnoredDownloadsPath))
            {
                return Array.Empty<string>();
            }
            
            FileInfo fileInfo = new(_config.IgnoredDownloadsPath);
            
            if (fileInfo.LastWriteTime > _lastModified)
            {
                string[] ignoredDownloads = (await File.ReadAllLinesAsync(_config.IgnoredDownloadsPath))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();
                
                _cache.Set(CacheKeys.IgnoredDownloads(typeof(T).Name), ignoredDownloads);

                _lastModified = fileInfo.LastWriteTime;

                _logger.LogInformation("ignored downloads reloaded");

                return ignoredDownloads;
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"error while reading ignored downloads file: {_config.IgnoredDownloadsPath}");
        }

        return Array.Empty<string>();
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }
}