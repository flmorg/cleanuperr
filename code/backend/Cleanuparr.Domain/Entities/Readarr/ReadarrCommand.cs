namespace Cleanuparr.Domain.Entities.Readarr;

public sealed record ReadarrCommand
{
    public string Name { get; set; } = string.Empty;
    
    public List<long> BookIds { get; set; } = [];
} 