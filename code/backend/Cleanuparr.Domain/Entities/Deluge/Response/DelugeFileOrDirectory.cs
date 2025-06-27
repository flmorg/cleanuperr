using System.Text.Json.Serialization;

namespace Cleanuparr.Domain.Entities.Deluge.Response;

public class DelugeFileOrDirectory
{
    [JsonPropertyName("type")]
    public string Type { get; set; } // "file" or "dir"

    [JsonPropertyName("contents")]
    public Dictionary<string, DelugeFileOrDirectory>? Contents { get; set; } // Recursive property for directories

    [JsonPropertyName("index")]
    public required int Index { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("size")]
    public int? Size { get; set; }

    [JsonPropertyName("offset")]
    public int? Offset { get; set; }

    [JsonPropertyName("progress")]
    public double? Progress { get; set; }

    [JsonPropertyName("priority")]
    public required int Priority { get; set; }

    [JsonPropertyName("progresses")]
    public List<double> Progresses { get; set; }
}