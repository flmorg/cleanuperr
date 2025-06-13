using Common.Configuration.DownloadClient;
using Common.Enums;
using Infrastructure.Configuration;
using Infrastructure.Http;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Infrastructure.Tests.Http;

public class DynamicHttpClientProviderFixture : IDisposable
{
    public ILogger<DynamicHttpClientProvider> Logger { get; }
    
    public DynamicHttpClientProviderFixture()
    {
        Logger = Substitute.For<ILogger<DynamicHttpClientProvider>>();
    }
    
    public DynamicHttpClientProvider CreateSut()
    {
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var configManager = Substitute.For<IConfigManager>();
        var certificateValidationService = Substitute.For<CertificateValidationService>();

        return new DynamicHttpClientProvider(
            Logger,
            httpClientFactory,
            configManager,
            certificateValidationService);
    }
    
    public ClientConfig CreateQBitClientConfig()
    {
        return new ClientConfig
        {
            Id = Guid.NewGuid(),
            Name = "QBit Test",
            Type = DownloadClientType.QBittorrent,
            Enabled = true,
            Host = new("http://localhost:8080"),
            Username = "admin",
            Password = "adminadmin"
        };
    }
    
    public ClientConfig CreateTransmissionClientConfig()
    {
        return new ClientConfig
        {
            Id = Guid.NewGuid(),
            Name = "Transmission Test",
            Type = DownloadClientType.Transmission,
            Enabled = true,
            Host = new("http://localhost:9091"),
            Username = "admin",
            Password = "adminadmin",
            UrlBase = "transmission"
        };
    }
    
    public ClientConfig CreateDelugeClientConfig()
    {
        return new ClientConfig
        {
            Id = Guid.NewGuid(),
            Name = "Deluge Test",
            Type = DownloadClientType.Deluge,
            Enabled = true,
            Host = new("http://localhost:8112"),
            Username = "admin",
            Password = "deluge"
        };
    }
    
    public void Dispose()
    {
        // Cleanup if needed
    }
}
