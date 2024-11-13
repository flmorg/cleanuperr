using System.Text;
using Common.Configuration;
using Domain.Sonarr;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Infrastructure.Verticals.Arr;

public sealed class SonarrClient : ArrClient
{
    public SonarrClient(ILogger<SonarrClient> logger, IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }

    public override async Task RefreshItemsAsync(ArrInstance arrInstance, HashSet<int> itemIds)
    {
        foreach (int itemId in itemIds)
        {
            Uri uri = new(arrInstance.Url, "/api/v3/command");
            SonarrCommand command = new()
            {
                Name = "SeriesSearch",
                SeriesId = itemId
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
            
                _logger.LogInformation("series search triggered | {url} | series id: {id}", arrInstance.Url, itemId);
            }
            catch
            {
                _logger.LogError("series search failed | {url} | series id: {id}", arrInstance.Url, itemId);
                throw;
            }
        }
    }
}