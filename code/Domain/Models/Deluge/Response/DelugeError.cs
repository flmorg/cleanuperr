using Newtonsoft.Json;

namespace Domain.Models.Deluge.Response;

public sealed record DelugeError
{
    [JsonProperty(PropertyName = "message")]
    public String Message { get; set; }

    [JsonProperty(PropertyName = "code")]
    public int Code { get; set; }
}