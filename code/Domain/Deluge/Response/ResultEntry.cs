namespace Domain.Deluge.Response;

public sealed record ResultEntry
{
    public bool Success { get; set; }
    
    public string Hash { get; set; }
}