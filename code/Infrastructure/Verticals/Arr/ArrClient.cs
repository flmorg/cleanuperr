using Common.Configuration;
using Domain.Arr.Queue;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Infrastructure.Verticals.Arr;

public abstract class ArrClient
{
    private protected ILogger<ArrClient> _logger;
    private protected HttpClient _httpClient;
    
    protected ArrClient(ILogger<ArrClient> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public virtual async Task<QueueListResponse> GetQueueItemsAsync(ArrInstance arrInstance, int page)
    {
        Uri uri = new(arrInstance.Url, $"/api/v3/queue?page={page}&pageSize=200&sortKey=timeleft");

        using HttpRequestMessage request = new(HttpMethod.Get, uri);
        SetApiKey(request, arrInstance.ApiKey);
        
        using HttpResponseMessage response = await _httpClient.SendAsync(request);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            _logger.LogError("queue list failed | {uri}", uri);
            throw;
        }
        
        string responseBody = await response.Content.ReadAsStringAsync();
        QueueListResponse? queueResponse = JsonConvert.DeserializeObject<QueueListResponse>(responseBody);

        if (queueResponse is null)
        {
            throw new Exception($"unrecognized queue list response | {uri} | {responseBody}");
        }

        return queueResponse;
    }

    public virtual async Task DeleteQueueItemAsync(ArrInstance arrInstance, QueueRecord queueRecord)
    {
        Uri uri = new(arrInstance.Url, $"/api/v3/queue/{queueRecord.Id}?removeFromClient=true&blocklist=true&skipRedownload=true&changeCategory=false");
        
        using HttpRequestMessage request = new(HttpMethod.Delete, uri);
        SetApiKey(request, arrInstance.ApiKey);

        using HttpResponseMessage response = await _httpClient.SendAsync(request);

        try
        {
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("queue item deleted | {url} | {title}", arrInstance.Url, queueRecord.Title);
        }
        catch
        {
            _logger.LogError("queue delete failed | {uri} | {title}", uri, queueRecord.Title);
            throw;
        }
    }

    public abstract Task RefreshItemsAsync(ArrInstance arrInstance, HashSet<int> itemIds);

    protected virtual void SetApiKey(HttpRequestMessage request, string apiKey)
    {
        request.Headers.Add("x-api-key", apiKey);
    }
}