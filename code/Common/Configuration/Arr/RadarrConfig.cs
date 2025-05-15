namespace Common.Configuration.Arr;

public sealed class RadarrConfig : ArrConfig, IConfig
{
    public const string SectionName = "Radarr";
    
    public void Validate()
    {
        throw new NotImplementedException();
    }
}