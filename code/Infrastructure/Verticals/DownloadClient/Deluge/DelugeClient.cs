using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Common.Configuration;
using Common.Configuration.DownloadClient;
using Domain.Models.Deluge.Exceptions;
using Domain.Models.Deluge.Request;
using Domain.Models.Deluge.Response;
using Infrastructure.Verticals.DownloadClient.Deluge.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Infrastructure.Verticals.DownloadClient.Deluge;

public sealed class DelugeClient
{
    private readonly DelugeConfig _config;
    private readonly HttpClient _httpClient;
    
    public DelugeClient(IOptions<DelugeConfig> config, IHttpClientFactory httpClientFactory)
    {
        _config = config.Value;
        _httpClient = httpClientFactory.CreateClient(nameof(DelugeService));
    }
    
    public async Task<bool> LoginAsync()
    {
        return await SendRequest<bool>("auth.login", _config.Password);
    }

    public async Task<bool> Logout()
    {
        return await SendRequest<bool>("auth.delete_session");
    }

    public async Task<List<DelugeTorrent>> ListTorrents(Dictionary<string, string>? filters = null)
    {
        filters ??= new Dictionary<string, string>();
        var keys = typeof(DelugeTorrent).GetAllJsonPropertyFromType();
        Dictionary<string, DelugeTorrent> result =
            await SendRequest<Dictionary<string, DelugeTorrent>>("core.get_torrents_status", filters, keys);
        return result.Values.ToList();
    }

    public async Task<List<DelugeTorrentExtended>> ListTorrentsExtended(Dictionary<string, string>? filters = null)
    {
        filters ??= new Dictionary<string, string>();
        var keys = typeof(DelugeTorrentExtended).GetAllJsonPropertyFromType();
        Dictionary<string, DelugeTorrentExtended> result =
            await SendRequest<Dictionary<string, DelugeTorrentExtended>>("core.get_torrents_status", filters, keys);
        return result.Values.ToList();
    }

    public async Task<DelugeTorrent?> GetTorrent(string hash)
    {
        List<DelugeTorrent> torrents = await ListTorrents(new Dictionary<string, string>() { { "hash", hash } });
        return torrents.FirstOrDefault();
    }

    public async Task<DelugeTorrentExtended?> GetTorrentExtended(string hash)
    {
        List<DelugeTorrentExtended> torrents =
            await ListTorrentsExtended(new Dictionary<string, string> { { "hash", hash } });
        return torrents.FirstOrDefault();
    }

    public async Task<DelugeContents?> GetTorrentFiles(string hash)
    {
        return await SendRequest<DelugeContents?>("web.get_torrent_files", hash);
    }

    public async Task ChangeFilesPriority(string hash, List<int> priorities)
    {
        Dictionary<string, List<int>> filePriorities = new()
        {
            { "file_priorities", priorities }
        };

        await SendRequest<DelugeResponse<object>>("core.set_torrent_options", hash, filePriorities);
    }

    private async Task<String> PostJson(String json)
    {
        StringContent content = new StringContent(json);
        content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

        var responseMessage = await _httpClient.PostAsync(new Uri(_config.Url, "/json"), content);
        responseMessage.EnsureSuccessStatusCode();

        var responseJson = await responseMessage.Content.ReadAsStringAsync();
        return responseJson;
    }

    private DelugeRequest CreateRequest(string method, params object[] parameters)
    {
        if (String.IsNullOrWhiteSpace(method))
        {
            throw new ArgumentException(nameof(method));
        }
        
        return new DelugeRequest(1, method, parameters);
    }
    
    public async Task<T> SendRequest<T>(string method, params object[] parameters)
    {
        return await SendRequest<T>(CreateRequest(method, parameters));
    }

    public async Task<T> SendRequest<T>(DelugeRequest webRequest)
    {
        var requestJson = JsonConvert.SerializeObject(webRequest, Formatting.None, new JsonSerializerSettings
        {
            NullValueHandling = webRequest.NullValueHandling
        });

        var responseJson = await PostJson(requestJson);
        var settings = new JsonSerializerSettings
        {
            Error = (_, args) =>
            {
                // Suppress the error and continue
                args.ErrorContext.Handled = true;
            }
        };
        
        DelugeResponse<T>? webResponse = JsonConvert.DeserializeObject<DelugeResponse<T>>(responseJson, settings);

        if (webResponse?.Error != null)
        {
            throw new DelugeClientException(webResponse.Error.Message);
        }

        if (webResponse?.ResponseId != webRequest.RequestId)
        {
            throw new DelugeClientException("desync");
        }

        return webResponse.Result;
    }
}