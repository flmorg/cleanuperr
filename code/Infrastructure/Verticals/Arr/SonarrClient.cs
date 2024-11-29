using System.Text;
using Common.Configuration;
using Common.Configuration.Arr;
using Domain.Models.Arr;
using Domain.Models.Sonarr;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Infrastructure.Verticals.Arr;

public sealed class SonarrClient : ArrClient
{
    public SonarrClient(ILogger<SonarrClient> logger, IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
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

            try
            {
                response.EnsureSuccessStatusCode();
            
                _logger.LogInformation("{log}", GetSearchLog(sonarrConfig.SearchType, arrInstance.Url, command, true));
            }
            catch
            {
                _logger.LogError("{log}", GetSearchLog(sonarrConfig.SearchType, arrInstance.Url, command, false));
                throw;
            }
        }
    }

    private string GetSearchLog(SonarrSearchType searchType, Uri instanceUrl, SonarrCommand command, bool success)
    {
        string message = success ? "triggered" : "failed";
        
        return searchType switch
        {
            SonarrSearchType.Episode =>
                $"episodes search {message} | {instanceUrl} | episode ids: {string.Join(',', command.EpisodeIds)}",
            SonarrSearchType.Season =>
                $"season search {message} | {instanceUrl} | season: {command.SeasonNumber} series id: {command.SeriesId}",
            _ => $"series search {message} | {instanceUrl} | series id: {command.SeriesId}"
        };
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