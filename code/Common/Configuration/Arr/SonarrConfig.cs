namespace Common.Configuration.Arr;

public sealed class SonarrConfig : ArrConfig, IConfig
{
    public const string SectionName = "Sonarr";
    
    public SonarrSearchType SearchType { get; init; }
    
    public void Validate()
    {
        throw new NotImplementedException();
    }
}