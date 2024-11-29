namespace Common.Configuration.Arr;

public sealed record SonarrConfig : ArrConfig
{
    public const string SectionName = "Sonarr";
    
    public SonarrSearchType SearchType { get; init; }
}