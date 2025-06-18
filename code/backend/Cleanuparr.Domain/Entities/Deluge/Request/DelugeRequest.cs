using Newtonsoft.Json;

namespace Cleanuparr.Domain.Entities.Deluge.Request;

public class DelugeRequest
{
    [JsonProperty(PropertyName = "id")]
    public int RequestId { get; set; }

    [JsonProperty(PropertyName = "method")]
    public string Method { get; set; }

    [JsonProperty(PropertyName = "params")]
    public List<object> Params { get; set; }

    [JsonIgnore]
    public NullValueHandling NullValueHandling { get; set; }

    public DelugeRequest(int requestId, string method, params object[]? parameters)
    {
        RequestId = requestId;
        Method = method;
        Params = [];

        if (parameters != null)
        {
            Params.AddRange(parameters);
        }

        NullValueHandling = NullValueHandling.Include;
    }
}