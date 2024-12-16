using Common.Configuration.Arr;
using Common.Configuration.Logging;
using Common.Configuration.QueueCleaner;
using Domain.Enums;
using Domain.Models.Arr;
using Domain.Models.Arr.Queue;
using Infrastructure.Verticals.ItemStriker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Infrastructure.Verticals.Arr;

public abstract class ArrClient
{
    protected readonly ILogger<ArrClient> _logger;
    protected readonly HttpClient _httpClient;
    protected readonly LoggingConfig _loggingConfig;
    protected readonly QueueCleanerConfig _queueCleanerConfig;
    protected readonly Striker _striker;
    
    protected ArrClient(
        ILogger<ArrClient> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<LoggingConfig> loggingConfig,
        IOptions<QueueCleanerConfig> queueCleanerConfig,
        Striker striker
    )
    {
        _logger = logger;
        _striker = striker;
        _httpClient = httpClientFactory.CreateClient();
        _loggingConfig = loggingConfig.Value;
        _queueCleanerConfig = queueCleanerConfig.Value;
        _striker = striker;
    }

    public virtual async Task<QueueListResponse> GetQueueItemsAsync(ArrInstance arrInstance, int page)
    {
        Uri uri = new(arrInstance.Url, GetQueueUrlPath(page));

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

    public virtual bool ShouldRemoveFromQueue(QueueRecord record)
    {
        bool hasWarn() => record.TrackedDownloadStatus
            .Equals("warning", StringComparison.InvariantCultureIgnoreCase);
        bool isImportBlocked() => record.TrackedDownloadState
            .Equals("importBlocked", StringComparison.InvariantCultureIgnoreCase);
        bool isImportPending() => record.TrackedDownloadState
            .Equals("importPending", StringComparison.InvariantCultureIgnoreCase);

        if (hasWarn() && (isImportBlocked() || isImportPending()))
        {
            return _striker.StrikeAndCheckLimit(
                record.DownloadId,
                record.Title,
                _queueCleanerConfig.ImportFailedMaxStrikes,
                StrikeType.ImportFailed
            );
        }

        return false;
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

    public abstract Task RefreshItemsAsync(ArrInstance arrInstance, ArrConfig config, HashSet<SearchItem>? items);

    public virtual bool IsRecordValid(QueueRecord record)
    {
        if (string.IsNullOrEmpty(record.DownloadId))
        {
            _logger.LogDebug("skip | download id is null for {title}", record.Title);
            return false;
        }

        if (record.DownloadId.Equals(record.Title, StringComparison.InvariantCultureIgnoreCase))
        {
            _logger.LogDebug("skip | item is not ready yet | {title}", record.Title);
            return false;
        }

        return true;
    }
    
    protected abstract string GetQueueUrlPath(int page);
    
    protected virtual void SetApiKey(HttpRequestMessage request, string apiKey)
    {
        request.Headers.Add("x-api-key", apiKey);
    }
}