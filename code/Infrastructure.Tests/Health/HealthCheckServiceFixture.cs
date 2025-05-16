using Common.Configuration.DownloadClient;
using Common.Enums;
using Infrastructure.Configuration;
using Infrastructure.Health;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.DownloadClient.Factory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Infrastructure.Tests.Health;

public class HealthCheckServiceFixture : IDisposable
{
    public ILogger<HealthCheckService> Logger { get; }
    public IConfigManager ConfigManager { get; }
    public IDownloadClientFactory ClientFactory { get; }
    public IDownloadService MockClient { get; }
    public DownloadClientConfig DownloadClientConfig { get; }

    public HealthCheckServiceFixture()
    {
        Logger = Substitute.For<ILogger<HealthCheckService>>();
        ConfigManager = Substitute.For<IConfigManager>();
        ClientFactory = Substitute.For<IDownloadClientFactory>();
        MockClient = Substitute.For<IDownloadService>();
        
        // Set up test download client config
        DownloadClientConfig = new DownloadClientConfig
        {
            Clients = new List<ClientConfig>
            {
                new()
                {
                    Id = "qbit1",
                    Name = "Test QBittorrent",
                    Type = DownloadClientType.QBittorrent,
                    Enabled = true,
                    Username = "admin",
                    Password = "adminadmin"
                },
                new()
                {
                    Id = "transmission1",
                    Name = "Test Transmission",
                    Type = DownloadClientType.Transmission,
                    Enabled = true,
                    Username = "admin",
                    Password = "adminadmin"
                },
                new()
                {
                    Id = "disabled1",
                    Name = "Disabled Client",
                    Type = DownloadClientType.QBittorrent,
                    Enabled = false,
                }
            }
        };
        
        // Set up the mock client factory
        ClientFactory.GetClient(Arg.Any<string>()).Returns(MockClient);
        MockClient.GetClientId().Returns("qbit1");
        
        // Set up mock config manager
        ConfigManager.GetDownloadClientConfigAsync().Returns(DownloadClientConfig);
    }

    public HealthCheckService CreateSut()
    {
        return new HealthCheckService(Logger, ConfigManager, ClientFactory);
    }
    
    public void SetupHealthyClient(string clientId)
    {
        // Setup a client that will successfully login
        MockClient.LoginAsync().Returns(Task.CompletedTask);
    }
    
    public void SetupUnhealthyClient(string clientId, string errorMessage = "Failed to connect")
    {
        // Setup a client that will fail to login
        MockClient.LoginAsync().Throws(new Exception(errorMessage));
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
