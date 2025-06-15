namespace Common.Configuration.Arr;

public sealed class SonarrConfig : ArrConfig
{
    public SonarrSearchType SearchType { get; set; } = SonarrSearchType.Episode;
    
    public override void Validate()
    {
    }
}