namespace Domain.Models.Arr;

public sealed class SonarrSearchItem : SearchItem
{
    public long SeriesId { get; set; }
    
    public override bool Equals(object? obj)
    {
        if (obj is not SonarrSearchItem other)
        {
            return false;
        }
        
        return Id == other.Id && SeriesId == other.SeriesId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, SeriesId);
    }
}