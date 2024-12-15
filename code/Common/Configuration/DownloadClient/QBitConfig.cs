namespace Common.Configuration.DownloadClient;

public sealed class QBitConfig : IConfig
{
    public const string SectionName = "qBittorrent";
    
    public Uri? Url { get; init; }
    
    public string? Username { get; init; }
    
    public string? Password { get; init; }
    
    public void Validate()
    {
        if (Url is null)
        {
            throw new ArgumentNullException(nameof(Url));
        }
    }
}