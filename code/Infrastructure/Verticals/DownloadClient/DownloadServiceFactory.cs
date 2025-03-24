using Common.Configuration.DownloadClient;
using Infrastructure.Verticals.DownloadClient.Deluge;
using Infrastructure.Verticals.DownloadClient.QBittorrent;
using Infrastructure.Verticals.DownloadClient.Transmission;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.DownloadClient;

public sealed class DownloadServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Common.Enums.DownloadClient _downloadClient;
    
    public DownloadServiceFactory(IServiceProvider serviceProvider, IOptions<DownloadClientConfig> downloadClientConfig)
    {
        _serviceProvider = serviceProvider;
        _downloadClient = downloadClientConfig.Value.DownloadClient;
    }

    public IDownloadService CreateDownloadClient() =>
        _downloadClient switch
        {
            Common.Enums.DownloadClient.QBittorrent => _serviceProvider.GetRequiredService<QBitService>(),
            Common.Enums.DownloadClient.Deluge => _serviceProvider.GetRequiredService<DelugeService>(),
            Common.Enums.DownloadClient.Transmission => _serviceProvider.GetRequiredService<TransmissionService>(),
            Common.Enums.DownloadClient.None => _serviceProvider.GetRequiredService<DummyDownloadService>(),
            Common.Enums.DownloadClient.Disabled => _serviceProvider.GetRequiredService<DummyDownloadService>(),
            _ => throw new ArgumentOutOfRangeException()
        };
}