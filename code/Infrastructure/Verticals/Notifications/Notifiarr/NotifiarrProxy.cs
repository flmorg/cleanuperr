using System.Text;
using Common.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Infrastructure.Verticals.Notifications.Notifiarr;

public class NotifiarrProxy : INotifiarrProxy
{
    private readonly HttpClient _httpClient;

    private const string Url = "https://notifiarr.com/api/v1/notification/passthrough/";

    public NotifiarrProxy(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(Constants.HttpClientWithRetryName);
    }

    public async Task SendNotification(NotifiarrPayload payload, NotifiarrConfig config)
    {
        try
        {
            string content = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{Url}{config.ApiKey}");
            request.Method = HttpMethod.Post;
            request.Content = new StringContent(content, Encoding.UTF8, "application/json");
            
            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException exception)
        {
            if (exception.StatusCode is null)
            {
                throw new NotifiarrException("unable to send notification", exception);
            }
            
            switch ((int)exception.StatusCode)
            {
                case 401:
                    throw new NotifiarrException("unable to send notification | API key is invalid");
                case 502:
                case 503:
                case 504:
                    throw new NotifiarrException("unable to send notification | service unavailable", exception);
                default:
                    throw new NotifiarrException("unable to send notification", exception);
            }
        }
    }
}