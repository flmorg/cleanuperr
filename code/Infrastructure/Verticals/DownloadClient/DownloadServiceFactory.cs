using Common.Configuration;
using Common.Configuration.DownloadClient;
using Infrastructure.Verticals.DownloadClient.Deluge;
using Infrastructure.Verticals.DownloadClient.QBittorrent;
using Infrastructure.Verticals.DownloadClient.Transmission;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.DownloadClient;

public sealed class DownloadServiceFactory
{
    private readonly QBitConfig _qBitConfig;
    private readonly DelugeConfig _delugeConfig;
    private readonly TransmissionConfig _transmissionConfig;
    private readonly IServiceProvider _serviceProvider;
    
    public DownloadServiceFactory(
        IOptions<QBitConfig> qBitConfig,
        IOptions<DelugeConfig> delugeConfig,
        IOptions<TransmissionConfig> transmissionConfig,
        IServiceProvider serviceProvider)
    {
        _qBitConfig = qBitConfig.Value;
        _delugeConfig = delugeConfig.Value;
        _transmissionConfig = transmissionConfig.Value;
        _serviceProvider = serviceProvider;
        
        _qBitConfig.Validate();
        _delugeConfig.Validate();
        _transmissionConfig.Validate();

        int enabledCount = new[] { _qBitConfig.Enabled, _delugeConfig.Enabled, _transmissionConfig.Enabled }
            .Count(enabled => enabled);

        if (enabledCount > 1)
        {
            throw new Exception("only one download client can be enabled");
        }

        if (enabledCount == 0)
        {
            throw new Exception("no download client is enabled");
        }
    }

    public IDownloadService CreateDownloadClient()
    {
        if (_qBitConfig.Enabled)
        {
            return _serviceProvider.GetRequiredService<QBitService>();
        }

        if (_delugeConfig.Enabled)
        {
            return _serviceProvider.GetRequiredService<DelugeService>();
        }

        if (_transmissionConfig.Enabled)
        {
            return _serviceProvider.GetRequiredService<TransmissionService>();
        }

        throw new NotSupportedException();
    }
}