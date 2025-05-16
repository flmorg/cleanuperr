using Common.Configuration.DownloadClient;
using Common.Enums;
using Infrastructure.Configuration;
using Infrastructure.Http;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.DownloadClient.Deluge;
using Infrastructure.Verticals.DownloadClient.Factory;
using Infrastructure.Verticals.DownloadClient.QBittorrent;
using Infrastructure.Verticals.DownloadClient.Transmission;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Infrastructure.Tests.Verticals.DownloadClient.Factory;

public class DownloadClientFactoryFixture : IDisposable
{
    public ILogger<DownloadClientFactory> Logger { get; }
    public IConfigManager ConfigManager { get; }
    public IServiceProvider ServiceProvider { get; }
    public DownloadClientConfig DownloadClientConfig { get; }

    public DownloadClientFactoryFixture()
    {
        Logger = Substitute.For<ILogger<DownloadClientFactory>>();
        ConfigManager = Substitute.For<IConfigManager>();
        
        // Set up test download client config
        DownloadClientConfig = new DownloadClientConfig
        {
            Clients = new List<ClientConfig>
            {
                new()
                {
                    Id = "qbit1",
                    Name = "Test QBittorrent",
                    Type = DownloadClient.QBittorrent,
                    Enabled = true,
                    Url = "http://localhost:8080",
                    Username = "admin",
                    Password = "adminadmin"
                },
                new()
                {
                    Id = "transmission1",
                    Name = "Test Transmission",
                    Type = DownloadClient.Transmission,
                    Enabled = true,
                    Url = "http://localhost:9091",
                    Username = "admin",
                    Password = "adminadmin"
                },
                new()
                {
                    Id = "deluge1",
                    Name = "Test Deluge",
                    Type = DownloadClient.Deluge,
                    Enabled = true,
                    Url = "http://localhost:8112",
                    Username = "admin",
                    Password = "adminadmin"
                },
                new()
                {
                    Id = "disabled1",
                    Name = "Disabled Client",
                    Type = DownloadClient.QBittorrent,
                    Enabled = false,
                    Url = "http://localhost:5555"
                }
            }
        };
        
        // Configure the ConfigManager to return our test config
        ConfigManager.GetDownloadClientConfigAsync().Returns(Task.FromResult(DownloadClientConfig));
        
        // Set up mock services
        var serviceCollection = new ServiceCollection();
        
        // Mock the services that will be resolved
        var qbitService = Substitute.For<QBitService>();
        var transmissionService = Substitute.For<TransmissionService>();
        var delugeService = Substitute.For<DelugeService>();
        var httpClientProvider = Substitute.For<IDynamicHttpClientProvider>();
        
        // Register our mock services in the service collection
        serviceCollection.AddSingleton(qbitService);
        serviceCollection.AddSingleton(transmissionService);
        serviceCollection.AddSingleton(delugeService);
        serviceCollection.AddSingleton(httpClientProvider);
        
        // Build the service provider
        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    public DownloadClientFactory CreateSut()
    {
        return new DownloadClientFactory(
            Logger,
            ConfigManager,
            ServiceProvider
        );
    }

    public void Dispose()
    {
        // Clean up if needed
    }
}
