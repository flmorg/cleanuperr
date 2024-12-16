namespace Domain.Models.Deluge.Response;

public sealed record TorrentStatus
{
    public string? Hash { get; set; }
    
    public string? State { get; set; }
    
    public string? Name { get; set; }
    
    public ulong Eta { get; set; }
}