namespace Domain.Models.Deluge.Response;

public sealed record DelugeMinimalStatus
{
    public string? Hash { get; set; }
}