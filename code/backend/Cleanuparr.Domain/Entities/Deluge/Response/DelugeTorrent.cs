using Newtonsoft.Json;

namespace Cleanuparr.Domain.Entities.Deluge.Response;

public record DelugeTorrent
{
    [JsonProperty(PropertyName = "comment")]
    public string Comment { get; set; }

    [JsonProperty(PropertyName = "is_seed")]
    public bool IsSeed { get; set; }

    [JsonProperty(PropertyName = "hash")]
    public string Hash { get; set; }

    [JsonProperty(PropertyName = "paused")]
    public bool Paused { get; set; }

    [JsonProperty(PropertyName = "ratio")]
    public double Ratio { get; set; }

    [JsonProperty(PropertyName = "message")]
    public string Message { get; set; }

    [JsonProperty(PropertyName = "name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "label")]
    public string Label { get; set; }
}