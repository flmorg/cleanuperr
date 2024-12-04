using System.Text;
using Common.Configuration.Arr;
using Common.Configuration.Logging;
using Domain.Models.Arr;
using Domain.Models.Sonarr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Infrastructure.Verticals.Arr;

public sealed class SonarrClient : ArrClient
{
    public SonarrClient(
        ILogger<SonarrClient> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<LoggingConfig> loggingConfig
    ) : base(logger, httpClientFactory, loggingConfig)
    {
    }

    public override async Task RefreshItemsAsync(ArrInstance arrInstance, ArrConfig config, HashSet<SearchItem>? items)
    {
        if (items?.Count is null or 0)
        {
            return;
        }

        SonarrConfig sonarrConfig = (SonarrConfig)config;
        
        Uri uri = new(arrInstance.Url, "/api/v3/command");
        
        foreach (SonarrCommand command in GetSearchCommands(sonarrConfig.SearchType, items))
        {
            using HttpRequestMessage request = new(HttpMethod.Post, uri);
            request.Content = new StringContent(
                JsonConvert.SerializeObject(command, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                Encoding.UTF8,
                "application/json"
            );
            SetApiKey(request, arrInstance.ApiKey);

            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            string? logContext = await ComputeCommandLogContextAsync(arrInstance, command, sonarrConfig.SearchType);

            try
            {
                response.EnsureSuccessStatusCode();
            
                _logger.LogInformation("{log}", GetSearchLog(sonarrConfig.SearchType, arrInstance.Url, command, true, logContext));
            }
            catch
            {
                _logger.LogError("{log}", GetSearchLog(sonarrConfig.SearchType, arrInstance.Url, command, false, logContext));
                throw;
            }
        }
    }

    private static string GetSearchLog(
        SonarrSearchType searchType,
        Uri instanceUrl,
        SonarrCommand command,
        bool success,
        string? logContext
    )
    {
        string status = success ? "triggered" : "failed";
        
        return searchType switch
        {
            SonarrSearchType.Episode =>
                $"episodes search {status} | {instanceUrl} | {logContext ?? $"episode ids: {string.Join(',', command.EpisodeIds)}"}",
            SonarrSearchType.Season =>
                $"season search {status} | {instanceUrl} | {logContext ?? $"season: {command.SeasonNumber} series id: {command.SeriesId}"}",
            SonarrSearchType.Series => $"series search {status} | {instanceUrl} | {logContext ?? $"series id: {command.SeriesId}"}",
            _ => throw new ArgumentOutOfRangeException(nameof(searchType), searchType, null)
        };
    }

    private async Task<string?> ComputeCommandLogContextAsync(ArrInstance arrInstance, SonarrCommand command, SonarrSearchType searchType)
    {
        try
        {
            if (!_loggingConfig.Enhanced)
            {
                return null;
            }
            
            StringBuilder log = new();

            if (searchType is SonarrSearchType.Episode)
            {
                var episodes = await GetEpisodesAsync(arrInstance, command.EpisodeIds);

                if (episodes?.Count is null or 0)
                {
                    return null;
                }

                var seriesIds = episodes
                    .Select(x => x.SeriesId)
                    .Distinct()
                    .ToList();

                List<Series> series = [];

                foreach (long id in seriesIds)
                {
                    Series? show = await GetSeriesAsync(arrInstance, id);

                    if (show is null)
                    {
                        return null;
                    }

                    series.Add(show);
                }

                foreach (var group in command.EpisodeIds.GroupBy(id => episodes.First(x => x.Id == id).SeriesId))
                {
                    var show = series.First(x => x.Id == group.Key);
                    var episode = episodes
                        .Where(ep => group.Any(x => x == ep.Id))
                        .OrderBy(x => x.SeasonNumber)
                        .ThenBy(x => x.EpisodeNumber)
                        .Select(x => $"S{x.SeasonNumber.ToString().PadLeft(2, '0')}E{x.EpisodeNumber.ToString().PadLeft(2, '0')}")
                        .ToList();

                    log.Append($"[{show.Title} {string.Join(',', episode)}]");
                }
            }

            if (searchType is SonarrSearchType.Season)
            {
                Series? show = await GetSeriesAsync(arrInstance, command.SeriesId.Value);

                if (show is null)
                {
                    return null;
                }

                log.Append($"[{show.Title} season {command.SeasonNumber}]");
            }

            if (searchType is SonarrSearchType.Series)
            {
                Series? show = await GetSeriesAsync(arrInstance, command.SeriesId.Value);

                if (show is null)
                {
                    return null;
                }

                log.Append($"[{show.Title}]");
            }

            return log.ToString();
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "failed to compute log context");
        }

        return null;
    }

    private async Task<List<Episode>?> GetEpisodesAsync(ArrInstance arrInstance, List<long> episodeIds)
    {
        Uri uri = new(arrInstance.Url, $"api/v3/episode?{string.Join('&', episodeIds.Select(x => $"episodeIds={x}"))}");
        using HttpRequestMessage request = new(HttpMethod.Get, uri);
        SetApiKey(request, arrInstance.ApiKey);

        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
                
        string responseBody = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<List<Episode>>(responseBody);
    }

    private async Task<Series?> GetSeriesAsync(ArrInstance arrInstance, long seriesId)
    {
        Uri uri = new(arrInstance.Url, $"api/v3/series/{seriesId}");
        using HttpRequestMessage request = new(HttpMethod.Get, uri);
        SetApiKey(request, arrInstance.ApiKey);

        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
                
        string responseBody = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<Series>(responseBody);
    }

    private List<SonarrCommand> GetSearchCommands(SonarrSearchType searchType, HashSet<SearchItem> items)
    {
        const string episodeSearch = "EpisodeSearch";
        const string seasonSearch = "SeasonSearch";
        const string seriesSearch = "SeriesSearch";
        
        List<SonarrCommand> commands = new();

        foreach (SearchItem item in items)
        {
            SonarrCommand command = searchType is SonarrSearchType.Episode
                ? commands.FirstOrDefault() ?? new() { Name = episodeSearch, EpisodeIds = new() }
                : new();
            
            switch (searchType)
            {
                case SonarrSearchType.Episode when command.EpisodeIds is null:
                    command.EpisodeIds = [item.Id];
                    break;
                
                case SonarrSearchType.Episode when command.EpisodeIds is not null:
                    command.EpisodeIds.Add(item.Id);
                    break;
                
                case SonarrSearchType.Season:
                    command.Name = seasonSearch;
                    command.SeasonNumber = item.Id;
                    command.SeriesId = ((SonarrSearchItem)item).SeriesId;
                    break;
                
                case SonarrSearchType.Series:
                    command.Name = seriesSearch;
                    command.SeriesId = item.Id;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(searchType), searchType, null);
            }

            if (searchType is SonarrSearchType.Episode && commands.Count > 0)
            {
                // only one command will be generated for episodes search
                continue;
            }
            
            commands.Add(command);
        }
        
        return commands;
    }
}