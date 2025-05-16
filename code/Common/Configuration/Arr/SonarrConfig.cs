namespace Common.Configuration.Arr;

public sealed class SonarrConfig : ArrConfig
{
    public SonarrSearchType SearchType { get; init; } = SonarrSearchType.Episode;
    
    public override void Validate()
    {
    }
}