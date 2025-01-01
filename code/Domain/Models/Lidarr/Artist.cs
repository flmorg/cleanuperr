namespace Domain.Models.Lidarr;

public sealed record Artist
{
    public int Id { get; set; }
    
    public string ArtistName { get; set; }
}