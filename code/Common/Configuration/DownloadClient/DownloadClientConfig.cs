using Microsoft.Extensions.Configuration;

namespace Common.Configuration.DownloadClient;

public sealed record DownloadClientConfig
{
    [ConfigurationKeyName("DOWNLOAD_CLIENT")]
    public Enums.DownloadClient DownloadClient { get; init; } = Enums.DownloadClient.QBittorrent;
}