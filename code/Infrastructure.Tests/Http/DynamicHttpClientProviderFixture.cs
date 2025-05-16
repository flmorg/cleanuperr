using Common.Configuration.DownloadClient;
using Common.Enums;
using Infrastructure.Http;
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
        return new DynamicHttpClientProvider(Logger);
    }
    
    public ClientConfig CreateQBitClientConfig()
    {
        return new ClientConfig
        {
            Id = "qbit-test",
            Name = "QBit Test",
            Type = DownloadClient.QBittorrent,
            Enabled = true,
            Url = "http://localhost:8080",
            Username = "admin",
            Password = "adminadmin"
        };
    }
    
    public ClientConfig CreateTransmissionClientConfig()
    {
        return new ClientConfig
        {
            Id = "transmission-test",
            Name = "Transmission Test",
            Type = DownloadClient.Transmission,
            Enabled = true,
            Url = "http://localhost:9091",
            Username = "admin",
            Password = "adminadmin",
            UrlBase = "transmission"
        };
    }
    
    public ClientConfig CreateDelugeClientConfig()
    {
        return new ClientConfig
        {
            Id = "deluge-test",
            Name = "Deluge Test",
            Type = DownloadClient.Deluge,
            Enabled = true,
            Url = "http://localhost:8112",
            Username = "admin",
            Password = "deluge"
        };
    }
    
    public void Dispose()
    {
        // Cleanup if needed
    }
}
