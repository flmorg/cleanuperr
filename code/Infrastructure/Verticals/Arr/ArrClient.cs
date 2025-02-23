using Common.Attributes;
using Common.Configuration.Arr;
using Common.Configuration.Logging;
using Common.Configuration.QueueCleaner;
using Common.Helpers;
using Domain.Enums;
using Domain.Models.Arr;
using Domain.Models.Arr.Queue;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.Arr.Interfaces;
using Infrastructure.Verticals.ItemStriker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Infrastructure.Verticals.Arr;

public abstract class ArrClient : IArrClient
{
    protected readonly ILogger<ArrClient> _logger;
    protected readonly HttpClient _httpClient;
    protected readonly LoggingConfig _loggingConfig;
    protected readonly QueueCleanerConfig _queueCleanerConfig;
    protected readonly IStriker _striker;
    protected readonly IDryRunInterceptor _dryRunInterceptor;
    
    protected ArrClient(
        ILogger<ArrClient> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<LoggingConfig> loggingConfig,
        IOptions<QueueCleanerConfig> queueCleanerConfig,
        IStriker striker,
        IDryRunInterceptor dryRunInterceptor
    )
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(Constants.HttpClientWithRetryName);
        _loggingConfig = loggingConfig.Value;
        _queueCleanerConfig = queueCleanerConfig.Value;
        _striker = striker;
        _dryRunInterceptor = dryRunInterceptor;
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

    public virtual async Task<bool> ShouldRemoveFromQueue(InstanceType instanceType, QueueRecord record, bool isPrivateDownload)
    {
        if (_queueCleanerConfig.ImportFailedIgnorePrivate && isPrivateDownload)
        {
            // ignore private trackers
            _logger.LogDebug("skip failed import check | download is private | {name}", record.Title);
            return false;
        }
        
        bool hasWarn() => record.TrackedDownloadStatus
            .Equals("warning", StringComparison.InvariantCultureIgnoreCase);
        bool isImportBlocked() => record.TrackedDownloadState
            .Equals("importBlocked", StringComparison.InvariantCultureIgnoreCase);
        bool isImportPending() => record.TrackedDownloadState
            .Equals("importPending", StringComparison.InvariantCultureIgnoreCase);
        bool isImportFailed() => record.TrackedDownloadState
            .Equals("importFailed", StringComparison.InvariantCultureIgnoreCase);
        bool isFailedLidarr() => instanceType is InstanceType.Lidarr &&
                                 (record.Status.Equals("failed", StringComparison.InvariantCultureIgnoreCase) ||
                                  record.Status.Equals("completed", StringComparison.InvariantCultureIgnoreCase)) &&
                                 hasWarn();
        
        if (hasWarn() && (isImportBlocked() || isImportPending() || isImportFailed()) || isFailedLidarr())
        {
            if (HasIgnoredPatterns(record))
            {
                _logger.LogDebug("skip failed import check | contains ignored pattern | {name}", record.Title);
                return false;
            }
            
            return await _striker.StrikeAndCheckLimit(
                record.DownloadId,
                record.Title,
                _queueCleanerConfig.ImportFailedMaxStrikes,
                StrikeType.ImportFailed
            );
        }

        return false;
    }
    
    public virtual async Task DeleteQueueItemAsync(ArrInstance arrInstance, QueueRecord record, bool removeFromClient)
    {
        Uri uri = new(arrInstance.Url, GetQueueDeleteUrlPath(record.Id, removeFromClient));

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Delete, uri);
            SetApiKey(request, arrInstance.ApiKey);

            HttpResponseMessage? response = await _dryRunInterceptor.InterceptAsync<HttpResponseMessage>(SendRequestAsync, request);
            response?.Dispose();
            
            _logger.LogInformation(
                removeFromClient
                    ? "queue item deleted | {url} | {title}"
                    : "queue item removed from arr | {url} | {title}",
                arrInstance.Url,
                record.Title
            );
        }
        catch
        {
            _logger.LogError("queue delete failed | {uri} | {title}", uri, record.Title);
            throw;
        }
    }

    public abstract Task RefreshItemsAsync(ArrInstance arrInstance, HashSet<SearchItem>? items);

    public virtual bool IsRecordValid(QueueRecord record)
    {
        if (string.IsNullOrEmpty(record.DownloadId))
        {
            _logger.LogDebug("skip | download id is null for {title}", record.Title);
            return false;
        }

        return true;
    }
    
    protected abstract string GetQueueUrlPath(int page);

    protected abstract string GetQueueDeleteUrlPath(long recordId, bool removeFromClient);
    
    protected virtual void SetApiKey(HttpRequestMessage request, string apiKey)
    {
        request.Headers.Add("x-api-key", apiKey);
    }

    [DryRunSafeguard]
    protected virtual async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
    {
        HttpResponseMessage response = await _httpClient.SendAsync(request);
        
        response.EnsureSuccessStatusCode();
        
        return response;
    }
    
    private bool HasIgnoredPatterns(QueueRecord record)
    {
        if (_queueCleanerConfig.ImportFailedIgnorePatterns?.Count is null or 0)
        {
            // no patterns are configured
            return false;
        }
            
        if (record.StatusMessages?.Count is null or 0)
        {
            // no status message found
            return false;
        }
        
        HashSet<string> messages = record.StatusMessages
            .SelectMany(x => x.Messages ?? Enumerable.Empty<string>())
            .ToHashSet();
        record.StatusMessages.Select(x => x.Title)
            .ToList()
            .ForEach(x => messages.Add(x));
        
        return messages.Any(
            m => _queueCleanerConfig.ImportFailedIgnorePatterns.Any(
                p => !string.IsNullOrWhiteSpace(p.Trim()) && m.Contains(p, StringComparison.InvariantCultureIgnoreCase)
            )
        );
    }
}