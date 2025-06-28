namespace Cleanuparr.Domain.Entities.Readarr;

public sealed record Book
{
    public required long Id { get; init; }
    
    public required string Title { get; init; }
    
    public long AuthorId { get; set; }
    
    public Author Author { get; set; } = new();
} 