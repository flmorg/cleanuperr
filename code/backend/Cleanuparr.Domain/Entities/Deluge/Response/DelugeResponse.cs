using Newtonsoft.Json;

namespace Cleanuparr.Domain.Entities.Deluge.Response;

public sealed record DelugeResponse<T>
{
    [JsonProperty(PropertyName = "id")]
    public int ResponseId { get; set; }

    [JsonProperty(PropertyName = "result")]
    public T? Result { get; set; }

    [JsonProperty(PropertyName = "error")]
    public DelugeError? Error { get; set; }
}