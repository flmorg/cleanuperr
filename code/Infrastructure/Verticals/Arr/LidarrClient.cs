using System.Text;
using Common.Configuration.Arr;
using Common.Configuration.Logging;
using Common.Configuration.QueueCleaner;
using Domain.Models.Arr;
using Domain.Models.Arr.Queue;
using Domain.Models.Lidarr;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.Arr.Interfaces;
using Infrastructure.Verticals.ItemStriker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Infrastructure.Verticals.Arr;

public class LidarrClient : ArrClient, ILidarrClient
{
    public LidarrClient(
        ILogger<LidarrClient> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<LoggingConfig> loggingConfig,
        IOptions<QueueCleanerConfig> queueCleanerConfig,
        IStriker striker,
        IDryRunInterceptor dryRunInterceptor
    ) : base(logger, httpClientFactory, loggingConfig, queueCleanerConfig, striker, dryRunInterceptor)
    {
    }

    protected override string GetQueueUrlPath(int page)
    {
        return $"/api/v1/queue?page={page}&pageSize=200&includeUnknownArtistItems=true&includeArtist=true&includeAlbum=true";
    }

    protected override string GetQueueDeleteUrlPath(long recordId, bool removeFromClient)
    {
        string path = $"/api/v1/queue/{recordId}?blocklist=true&skipRedownload=true&changeCategory=false";

        path += removeFromClient ? "&removeFromClient=true" : "&removeFromClient=false";

        return path;
    }

    public override async Task RefreshItemsAsync(ArrInstance arrInstance, HashSet<SearchItem>? items)
    {
        if (items?.Count is null or 0) return;

        Uri uri = new(arrInstance.Url, "/api/v1/command");

        foreach (var command in GetSearchCommands(items))
        {
            using HttpRequestMessage request = new(HttpMethod.Post, uri);
            request.Content = new StringContent(
                JsonConvert.SerializeObject(command, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                Encoding.UTF8,
                "application/json"
            );
            SetApiKey(request, arrInstance.ApiKey);

            string? logContext = await ComputeCommandLogContextAsync(arrInstance, command);

            try
            {
                HttpResponseMessage? response = await _dryRunInterceptor.InterceptAsync<HttpResponseMessage>(SendRequestAsync, request);
                response?.Dispose();
                
                _logger.LogInformation("{log}", GetSearchLog(arrInstance.Url, command, true, logContext));
            }
            catch
            {
                _logger.LogError("{log}", GetSearchLog(arrInstance.Url, command, false, logContext));
                throw;
            }
        }
    }

    public override bool IsRecordValid(QueueRecord record)
    {
        if (record.ArtistId is 0 || record.AlbumId is 0)
        {
            _logger.LogDebug("skip | artist id and/or album id missing | {title}", record.Title);
            return false;
        }

        return base.IsRecordValid(record);
    }

    private static string GetSearchLog(
        Uri instanceUrl,
        LidarrCommand command,
        bool success,
        string? logContext
    )
    {
        string status = success ? "triggered" : "failed";

        return $"album search {status} | {instanceUrl} | {logContext ?? $"albums: {string.Join(',', command.AlbumIds)}"}";
    }

    private async Task<string?> ComputeCommandLogContextAsync(ArrInstance arrInstance, LidarrCommand command)
    {
        try
        {
            if (!_loggingConfig.Enhanced) return null;

            StringBuilder log = new();

            var albums = await GetAlbumsAsync(arrInstance, command.AlbumIds);

            if (albums?.Count is null or 0) return null;

            var groups = albums
                .GroupBy(x => x.Artist.Id)
                .ToList();

            foreach (var group in groups)
            {
                var first = group.First();

                log.Append($"[{first.Artist.ArtistName} albums {string.Join(',', group.Select(x => x.Title).ToList())}]");
            }

            return log.ToString();
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "failed to compute log context");
        }

        return null;
    }

    private async Task<List<Album>?> GetAlbumsAsync(ArrInstance arrInstance, List<long> albumIds)
    {
        Uri uri = new(arrInstance.Url, $"api/v1/album?{string.Join('&', albumIds.Select(x => $"albumIds={x}"))}");
        using HttpRequestMessage request = new(HttpMethod.Get, uri);
        SetApiKey(request, arrInstance.ApiKey);

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<List<Album>>(responseBody);
    }

    private List<LidarrCommand> GetSearchCommands(HashSet<SearchItem> items)
    {
        const string albumSearch = "AlbumSearch";

        return [new LidarrCommand { Name = albumSearch, AlbumIds = items.Select(i => i.Id).ToList() }];
    }
}