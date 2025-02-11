using Newtonsoft.Json;

namespace Domain.Models.Deluge.Response;

public sealed record TorrentStatus
{
    public string? Hash { get; init; }
    
    public string? State { get; init; }
    
    public string? Name { get; init; }
    
    public ulong Eta { get; init; }
    
    public bool Private { get; init; }
    
    [JsonProperty("total_done")]
    public long TotalDone { get; init; }
    
    public string? Label { get; init; }
    
    [JsonProperty("seeding_time")]
    public long SeedingTime { get; init; }
    
    public float Ratio { get; init; }
}