using Common.Configuration;
using Infrastructure.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Providers;

public sealed class IgnoredDownloadsProvider<T>
    where T : IIgnoredDownloadsConfig
{
    private readonly ILogger<IgnoredDownloadsProvider<T>> _logger;
    private IIgnoredDownloadsConfig _config;
    private readonly IMemoryCache _cache;
    private DateTime _lastModified = DateTime.MinValue;

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
    }

    public async Task<IReadOnlyList<string>> GetIgnoredDownloads()
    {
        if (string.IsNullOrEmpty(_config.IgnoredDownloadsPath))
        {
            return Array.Empty<string>();
        }

        FileInfo fileInfo = new(_config.IgnoredDownloadsPath);
        
        if (fileInfo.LastWriteTime > _lastModified ||
            !_cache.TryGetValue(CacheKeys.IgnoredDownloads(typeof(T).Name), out IReadOnlyList<string>? ignoredDownloads) ||
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