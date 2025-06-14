using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Common.Configuration.QueueCleaner;
using Common.Helpers;
using Data;
using Data.Enums;
using Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.ContentBlocker;

public sealed class BlocklistProvider
{
    private readonly ILogger<BlocklistProvider> _logger;
    private readonly DataContext _dataContext;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly Dictionary<InstanceType, string> _configHashes = new();
    private static DateTime _lastLoadTime = DateTime.MinValue;
    private const int LoadIntervalHours = 6;

    public BlocklistProvider(
        ILogger<BlocklistProvider> logger,
        DataContext dataContext,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory
    )
    {
        _logger = logger;
        _dataContext = dataContext;
        _cache = cache;
        _httpClient = httpClientFactory.CreateClient(Constants.HttpClientWithRetryName);
    }

    public async Task LoadBlocklistsAsync()
    {
        try
        {
            int changedCount = 0;
            var queueCleanerConfig = await _dataContext.QueueCleanerConfigs
                .AsNoTracking()
                .FirstAsync();
            bool shouldReload = false;

            if (_lastLoadTime.AddHours(LoadIntervalHours) < DateTime.UtcNow)
            {
                shouldReload = true;
                _lastLoadTime = DateTime.UtcNow;
            }
            
            if (!queueCleanerConfig.ContentBlocker.Enabled)
            {
                _logger.LogDebug("Content blocker is disabled, skipping blocklist loading");
                return;
            }
            
            // Check and update Sonarr blocklist if needed
            string sonarrHash = GenerateSettingsHash(queueCleanerConfig.ContentBlocker.Sonarr);
            if (shouldReload || !_configHashes.TryGetValue(InstanceType.Sonarr, out string? oldSonarrHash) || sonarrHash != oldSonarrHash)
            {
                _logger.LogDebug("Loading Sonarr blocklist");
                
                await LoadPatternsAndRegexesAsync(queueCleanerConfig.ContentBlocker.Sonarr, InstanceType.Sonarr);
                _configHashes[InstanceType.Sonarr] = sonarrHash;
                changedCount++;
            }
            
            // Check and update Radarr blocklist if needed
            string radarrHash = GenerateSettingsHash(queueCleanerConfig.ContentBlocker.Radarr);
            if (shouldReload || !_configHashes.TryGetValue(InstanceType.Radarr, out string? oldRadarrHash) || radarrHash != oldRadarrHash)
            {
                _logger.LogDebug("Loading Radarr blocklist");
                
                await LoadPatternsAndRegexesAsync(queueCleanerConfig.ContentBlocker.Radarr, InstanceType.Radarr);
                _configHashes[InstanceType.Radarr] = radarrHash;
                changedCount++;
            }
            
            // Check and update Lidarr blocklist if needed
            string lidarrHash = GenerateSettingsHash(queueCleanerConfig.ContentBlocker.Lidarr);
            if (shouldReload || !_configHashes.TryGetValue(InstanceType.Lidarr, out string? oldLidarrHash) || lidarrHash != oldLidarrHash)
            {
                _logger.LogDebug("Loading Lidarr blocklist");
                
                await LoadPatternsAndRegexesAsync(queueCleanerConfig.ContentBlocker.Lidarr, InstanceType.Lidarr);
                _configHashes[InstanceType.Lidarr] = lidarrHash;
                changedCount++;
            }
            
            if (changedCount > 0)
            {
                _logger.LogInformation("Successfully loaded {count} blocklists", changedCount);
            }
            else
            {
                _logger.LogTrace("All blocklists are already up to date");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load blocklists");
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
        if (string.IsNullOrEmpty(blocklistSettings.BlocklistPath))
        {
            return;
        }
        
        string[] filePatterns = await ReadContentAsync(blocklistSettings.BlocklistPath);
        
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

        _cache.Set(CacheKeys.BlocklistType(instanceType), blocklistSettings.BlocklistType);
        _cache.Set(CacheKeys.BlocklistPatterns(instanceType), patterns);
        _cache.Set(CacheKeys.BlocklistRegexes(instanceType), regexes);
        
        _logger.LogDebug("loaded {count} patterns", patterns.Count);
        _logger.LogDebug("loaded {count} regexes", regexes.Count);
        _logger.LogDebug("blocklist loaded in {elapsed} ms | {path}", elapsed.TotalMilliseconds, blocklistSettings.BlocklistPath);
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
    
    private string GenerateSettingsHash(BlocklistSettings blocklistSettings)
    {
        // Create a string that represents the relevant blocklist configuration
        var configStr = $"{blocklistSettings.BlocklistPath ?? string.Empty}|{blocklistSettings.BlocklistType}";
        
        // Create SHA256 hash of the configuration string
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(configStr);
        var hashBytes = sha.ComputeHash(bytes);
        
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}