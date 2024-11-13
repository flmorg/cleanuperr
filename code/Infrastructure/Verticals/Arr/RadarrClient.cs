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
        if (itemIds.Count is 0)
        {
            return;
        }
        
        Uri uri = new(arrInstance.Url, "/api/v3/command");
        RadarrCommand command = new()
        {
            Name = "MoviesSearch",
            MovieIds = itemIds
        };
        
        using HttpRequestMessage request = new(HttpMethod.Post, uri);
        request.Content = new StringContent(
            JsonConvert.SerializeObject(command),
            Encoding.UTF8,
            "application/json"
        );
        SetApiKey(request, arrInstance.ApiKey);

        using HttpResponseMessage response = await _httpClient.SendAsync(request);

        try
        {
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("movie search triggered | {url} | movie ids: {ids}", arrInstance.Url, string.Join(",", itemIds));
        }
        catch
        {
            _logger.LogError("movie search failed | {url} | movie ids: {ids}", arrInstance.Url, string.Join(",", itemIds));
            throw;
        }
    }
}