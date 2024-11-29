namespace Common.Configuration.DownloadClient;

public sealed class QBitConfig : IConfig
{
    public const string SectionName = "qBittorrent";
    
    public required bool Enabled { get; init; }
    
    public Uri? Url { get; init; }
    
    public string? Username { get; init; }
    
    public string? Password { get; init; }
    
    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (Url is null)
        {
            throw new ArgumentNullException(nameof(Url));
        }
    }
}