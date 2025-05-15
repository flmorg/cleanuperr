using Microsoft.Extensions.Configuration;

namespace Common.Configuration.DownloadClient;

public sealed record DownloadClientConfig : IConfig
{
    [ConfigurationKeyName("DOWNLOAD_CLIENT")]
    public Enums.DownloadClient DownloadClient { get; init; } = Enums.DownloadClient.None;
    
    public void Validate()
    {
        throw new NotImplementedException();
    }
}