namespace Domain.Models.Arr.Queue;

public record Image
{
    public required string CoverType { get; init; }
    
    public required Uri RemoteUrl { get; init; }
}