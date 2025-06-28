namespace Cleanuparr.Domain.Entities.Readarr;

public sealed record Author
{
    public long Id { get; set; }
    
    public string AuthorName { get; set; } = string.Empty;
} 