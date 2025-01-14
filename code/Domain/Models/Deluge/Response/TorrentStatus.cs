namespace Domain.Models.Deluge.Response;

public sealed record TorrentStatus
{
    public string? Hash { get; init; }
    
    public string? State { get; init; }
    
    public string? Name { get; init; }
    
    public ulong Eta { get; init; }
    
    public bool Private { get; init; }
}