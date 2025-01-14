using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Common.Configuration.Arr;
using Common.Configuration.ContentBlocker;
using Common.Helpers;
using Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.ContentBlocker;

public sealed class BlocklistProvider
{
    private readonly ILogger<BlocklistProvider> _logger;
    private readonly SonarrConfig _sonarrConfig;
    private readonly RadarrConfig _radarrConfig;
    private readonly LidarrConfig _lidarrConfig;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private bool _initialized;

    private const string Type = "type";
    private const string Patterns = "patterns";
    private const string Regexes = "regexes";

    public BlocklistProvider(
        ILogger<BlocklistProvider> logger,
        IOptions<SonarrConfig> sonarrConfig,
        IOptions<RadarrConfig> radarrConfig,
        IOptions<LidarrConfig> lidarrConfig,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory
    )
    {
        _logger = logger;
        _sonarrConfig = sonarrConfig.Value;
        _radarrConfig = radarrConfig.Value;
        _lidarrConfig = lidarrConfig.Value;
        _cache = cache;
        _httpClient = httpClientFactory.CreateClient(Constants.HttpClientWithRetryName);
    }

    public async Task LoadBlocklistsAsync()
    {
        if (_initialized)
        {
            _logger.LogDebug("blocklists already loaded");
            return;
        }
        
        try
        {
            await LoadPatternsAndRegexesAsync(_sonarrConfig.Block.Type, _sonarrConfig.Block.Path, InstanceType.Sonarr);
            await LoadPatternsAndRegexesAsync(_radarrConfig.Block.Type, _radarrConfig.Block.Path, InstanceType.Radarr);
            await LoadPatternsAndRegexesAsync(_lidarrConfig.Block.Type, _lidarrConfig.Block.Path, InstanceType.Lidarr);
            
            _initialized = true;
        }
        catch
        {
            _logger.LogError("failed to load blocklists");
            throw;
        }
    }

    public BlocklistType GetBlocklistType(InstanceType instanceType)
    {
        _cache.TryGetValue($"{instanceType.ToString()}_{Type}", out BlocklistType? blocklistType);

        return blocklistType ?? BlocklistType.Blacklist;
    }
    
    public ConcurrentBag<string> GetPatterns(InstanceType instanceType)
    {
        _cache.TryGetValue($"{instanceType.ToString()}_{Patterns}", out ConcurrentBag<string>? patterns);

        return patterns ?? [];
    }

    public ConcurrentBag<Regex> GetRegexes(InstanceType instanceType)
    {
        _cache.TryGetValue($"{instanceType.ToString()}_{Regexes}", out ConcurrentBag<Regex>? regexes);
        
        return regexes ?? [];
    }

    private async Task LoadPatternsAndRegexesAsync(BlocklistType blocklistType, string? blocklistPath, InstanceType instanceType)
    {
        if (string.IsNullOrEmpty(blocklistPath))
        {
            return;
        }
        
        string[] filePatterns = await ReadContentAsync(blocklistPath);
        
        long startTime = Stopwatch.GetTimestamp();
        ParallelOptions options = new() { MaxDegreeOfParallelism = 5 };
        const string regexId = "regex:";
        ConcurrentBag<string> patterns = [];
        ConcurrentBag<Regex> regexes = [];
        
        Parallel.ForEach(filePatterns, options, pattern =>
        {
            if (!pattern.StartsWith(regexId))
            {
                patterns.Add(pattern);
                return;
            }
            
            pattern = pattern[regexId.Length..];
            
            try
            {
                Regex regex = new(pattern, RegexOptions.Compiled);
                regexes.Add(regex);
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("invalid regex | {pattern}", pattern);
            }
        });

        TimeSpan elapsed = Stopwatch.GetElapsedTime(startTime);

        _cache.Set($"{instanceType.ToString()}_{Type}", blocklistType);
        _cache.Set($"{instanceType.ToString()}_{Patterns}", patterns);
        _cache.Set($"{instanceType.ToString()}_{Regexes}", regexes);
        
        _logger.LogDebug("loaded {count} patterns", patterns.Count);
        _logger.LogDebug("loaded {count} regexes", regexes.Count);
        _logger.LogDebug("blocklist loaded in {elapsed} ms | {path}", elapsed.TotalMilliseconds, blocklistPath);
    }
    
    private async Task<string[]> ReadContentAsync(string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            // http(s) url
            return await ReadFromUrlAsync(path);
        }

        if (File.Exists(path))
        {
            // local file path
            return await File.ReadAllLinesAsync(path);
        }

        throw new ArgumentException($"blocklist not found | {path}");
    }

    private async Task<string[]> ReadFromUrlAsync(string url)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        
        return (await response.Content.ReadAsStringAsync())
            .Split(['\r','\n'], StringSplitOptions.RemoveEmptyEntries);
    }
}