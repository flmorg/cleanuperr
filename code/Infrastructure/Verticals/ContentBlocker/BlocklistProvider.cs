using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Common.Configuration.ContentBlocker;
using Common.Helpers;
using Domain.Enums;
using Infrastructure.Configuration;
using Infrastructure.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.ContentBlocker;

public sealed class BlocklistProvider
{
    private readonly ILogger<BlocklistProvider> _logger;
    private readonly IConfigurationManager _configManager;
    private readonly ContentBlockerConfig _contentBlockerConfig;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private bool _initialized;

    public BlocklistProvider(
        ILogger<BlocklistProvider> logger,
        IConfigurationManager configManager,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory
    )
    {
        _logger = logger;
        _configManager = configManager;
        _cache = cache;
        _httpClient = httpClientFactory.CreateClient(Constants.HttpClientWithRetryName);
        
        // Get the content blocker config
        var configTask = _configManager.GetContentBlockerConfigAsync();
        configTask.Wait();
        _contentBlockerConfig = configTask.Result ?? new ContentBlockerConfig();
    }

    public async Task LoadBlocklistsAsync()
    {
        if (_initialized)
        {
            _logger.LogTrace("blocklists already loaded");
            return;
        }
        
        try
        {
            // Refresh the configuration before loading blocklists
            var configTask = _configManager.GetContentBlockerConfigAsync();
            await configTask;
            if (configTask.Result != null)
            {
                // Update the local config reference
                _contentBlockerConfig = configTask.Result;
            }
            
            await LoadPatternsAndRegexesAsync(_contentBlockerConfig.Sonarr, InstanceType.Sonarr);
            await LoadPatternsAndRegexesAsync(_contentBlockerConfig.Radarr, InstanceType.Radarr);
            await LoadPatternsAndRegexesAsync(_contentBlockerConfig.Lidarr, InstanceType.Lidarr);
            
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
        _cache.TryGetValue(CacheKeys.BlocklistType(instanceType), out BlocklistType? blocklistType);

        return blocklistType ?? BlocklistType.Blacklist;
    }
    
    public ConcurrentBag<string> GetPatterns(InstanceType instanceType)
    {
        _cache.TryGetValue(CacheKeys.BlocklistPatterns(instanceType), out ConcurrentBag<string>? patterns);

        return patterns ?? [];
    }

    public ConcurrentBag<Regex> GetRegexes(InstanceType instanceType)
    {
        _cache.TryGetValue(CacheKeys.BlocklistRegexes(instanceType), out ConcurrentBag<Regex>? regexes);
        
        return regexes ?? [];
    }

    private async Task LoadPatternsAndRegexesAsync(BlocklistSettings blocklistSettings, InstanceType instanceType)
    {
        if (!blocklistSettings.Enabled)
        {
            return;
        }
        
        if (string.IsNullOrEmpty(blocklistSettings.Path))
        {
            return;
        }
        
        string[] filePatterns = await ReadContentAsync(blocklistSettings.Path);
        
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

        _cache.Set(CacheKeys.BlocklistType(instanceType), blocklistSettings.Type);
        _cache.Set(CacheKeys.BlocklistPatterns(instanceType), patterns);
        _cache.Set(CacheKeys.BlocklistRegexes(instanceType), regexes);
        
        _logger.LogDebug("loaded {count} patterns", patterns.Count);
        _logger.LogDebug("loaded {count} regexes", regexes.Count);
        _logger.LogDebug("blocklist loaded in {elapsed} ms | {path}", elapsed.TotalMilliseconds, blocklistSettings.Path);
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