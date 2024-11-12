using System.Text;
using Common.Configuration;
using Domain.Radarr;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Infrastructure.Verticals.Arr;

public sealed class RadarrClient : ArrClient
{
    public RadarrClient(ILogger<ArrClient> logger, IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }

    public override async Task RefreshItemsAsync(ArrInstance arrInstance, HashSet<int> itemIds)
    {
        Uri uri = new(arrInstance.Url, "/api/v3/command");
        RadarrCommand command = new()
        {
            Name = "MoviesSearch",
            MovieIds = itemIds
        };
        
        using HttpRequestMessage sonarrRequest = new(HttpMethod.Post, uri);
        sonarrRequest.Content = new StringContent(
            JsonConvert.SerializeObject(command),
            Encoding.UTF8,
            "application/json"
        );
        sonarrRequest.Headers.Add("x-api-key", arrInstance.ApiKey);

        using HttpResponseMessage response = await _httpClient.SendAsync(sonarrRequest);

        try
        {
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("movie search triggered | movie ids: {ids}", string.Join(",", itemIds));
        }
        catch
        {
            _logger.LogError("series search failed | movie ids: {ids}", string.Join(",", itemIds));
            throw;
        }
    }
}