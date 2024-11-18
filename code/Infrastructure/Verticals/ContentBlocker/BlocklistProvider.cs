using System.Diagnostics;
using System.Text.RegularExpressions;
using Common.Configuration.ContentBlocker;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.ContentBlocker;

public sealed class BlocklistProvider
{
    private readonly ILogger<BlocklistProvider> _logger;
    private readonly ContentBlockerConfig _config;
    private readonly HttpClient _httpClient;
    
    public BlocklistType BlocklistType { get; }

    public List<string> Patterns { get; } = [];

    public List<Regex> Regexes { get; } = [];

    public BlocklistProvider(
        ILogger<BlocklistProvider> logger,
        IOptions<ContentBlockerConfig> config,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _config = config.Value;
        _httpClient = httpClientFactory.CreateClient();
        
        _config.Validate();
        
        if (_config.Blacklist?.Enabled is true)
        {
            BlocklistType = BlocklistType.Blacklist;
        }

        if (_config.Whitelist?.Enabled is true)
        {
            BlocklistType = BlocklistType.Whitelist;
        }
    }

    public async Task LoadBlocklistAsync()
    {
        if (Patterns.Count > 0 || Regexes.Count > 0)
        {
            _logger.LogDebug("blocklist already loaded");
            return;
        }
        
        try
        {
            await LoadPatternsAndRegexesAsync();
        }
        catch
        {
            _logger.LogError("failed to load {type}", BlocklistType.ToString());
            throw;
        }
    }

    private async Task LoadPatternsAndRegexesAsync()
    {
        string[] patterns;
        
        if (BlocklistType is BlocklistType.Blacklist)
        {
            patterns = await ReadContentAsync(_config.Blacklist.Path);
        }
        else
        {
            patterns = await ReadContentAsync(_config.Whitelist.Path);
        }
        
        long startTime = Stopwatch.GetTimestamp();
        ParallelOptions options = new() { MaxDegreeOfParallelism = 5 };
        
        Parallel.ForEach(patterns, options, pattern =>
        {
            try
            {
                Regex regex = new(pattern, RegexOptions.Compiled);
                Regexes.Add(regex);
            }
            catch (ArgumentException)
            {
                Patterns.Add(pattern);
            }
        });

        TimeSpan elapsed = Stopwatch.GetElapsedTime(startTime);
        
        _logger.LogDebug("loaded {count} patterns", Patterns.Count);
        _logger.LogDebug("loaded {count} regexes", Regexes.Count);
        _logger.LogDebug("blocklist loaded in {elapsed} ms", elapsed.TotalMilliseconds);
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