using Common.Configuration;
using Common.Configuration.DownloadClient;
using Infrastructure.Verticals.DownloadClient.Deluge;
using Infrastructure.Verticals.DownloadClient.QBittorrent;
using Infrastructure.Verticals.DownloadClient.Transmission;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.DownloadClient;

public sealed class DownloadServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Domain.Enums.DownloadClient _downloadClient;
    
    public DownloadServiceFactory(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _downloadClient = (Domain.Enums.DownloadClient)Enum.Parse(
            typeof(Domain.Enums.DownloadClient),
            configuration[EnvironmentVariables.DownloadClient] ?? Domain.Enums.DownloadClient.QBittorrent.ToString(),
            true
        );
    }

    public IDownloadService CreateDownloadClient() =>
        _downloadClient switch
        {
            Domain.Enums.DownloadClient.QBittorrent => _serviceProvider.GetRequiredService<QBitService>(),
            Domain.Enums.DownloadClient.Deluge => _serviceProvider.GetRequiredService<DelugeService>(),
            Domain.Enums.DownloadClient.Transmission => _serviceProvider.GetRequiredService<TransmissionService>(),
            _ => throw new ArgumentOutOfRangeException()
        };
}