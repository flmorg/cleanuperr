using System.Text.Json.Serialization;

namespace Cleanuparr.Domain.Entities.Deluge.Response;

public sealed record DelugeContents
{
    [JsonPropertyName("contents")]
    public Dictionary<string, DelugeFileOrDirectory>? Contents { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } // Always "dir" for the root
}