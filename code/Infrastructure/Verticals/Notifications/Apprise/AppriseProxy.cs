using System.Text;
using Common.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Infrastructure.Verticals.Notifications.Apprise;

public sealed class AppriseProxy : IAppriseProxy
{
    private readonly HttpClient _httpClient;

    public AppriseProxy(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(Constants.HttpClientWithRetryName);
    }

    public async Task SendNotification(ApprisePayload payload, AppriseConfig config)
    {
        string content = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
        
        UriBuilder uriBuilder = new(config.Url);
        uriBuilder.Path = $"{uriBuilder.Path.TrimEnd('/')}/notify/{config.Key}";
        
        using HttpRequestMessage request = new(HttpMethod.Post, uriBuilder.Uri);
        request.Method = HttpMethod.Post;
        request.Content = new StringContent(content, Encoding.UTF8, "application/json");
        
        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}