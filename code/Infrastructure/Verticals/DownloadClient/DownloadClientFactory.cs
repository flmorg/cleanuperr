using Common.Configuration;
using Infrastructure.Verticals.DownloadClient.Deluge;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.DownloadClient;

public sealed class DownloadClientFactory
{
    private readonly QBitConfig _qBitConfig;
    private readonly DelugeConfig _delugeConfig;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceProvider _serviceProvider;
    
    public DownloadClientFactory(
        IOptions<QBitConfig> qBitConfig,
        IOptions<DelugeConfig> delugeConfig,
        IServiceProvider serviceProvider)
    {
        _qBitConfig = qBitConfig.Value;
        _delugeConfig = delugeConfig.Value;
        _serviceProvider = serviceProvider;
        
        _qBitConfig.Validate();
        _delugeConfig.Validate();

        if (_qBitConfig.Enabled && _delugeConfig.Enabled)
        {
            throw new Exception("only one download client can be enabled");
        }

        if (!_qBitConfig.Enabled && !_delugeConfig.Enabled)
        {
            throw new Exception("no download client is enabled");
        }
    }

    public IDownloadClient CreateDownloadClient()
    {
        if (_qBitConfig.Enabled)
        {
            return _serviceProvider.GetRequiredService<QBitClient>();
        }

        if (_delugeConfig.Enabled)
        {
            return _serviceProvider.GetRequiredService<DelugeClient>();
        }

        throw new NotSupportedException();
    }
}