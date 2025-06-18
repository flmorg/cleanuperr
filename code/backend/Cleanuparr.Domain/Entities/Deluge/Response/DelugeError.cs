using Newtonsoft.Json;

namespace Cleanuparr.Domain.Entities.Deluge.Response;

public sealed record DelugeError
{
    [JsonProperty(PropertyName = "message")]
    public String Message { get; set; }

    [JsonProperty(PropertyName = "code")]
    public int Code { get; set; }
}