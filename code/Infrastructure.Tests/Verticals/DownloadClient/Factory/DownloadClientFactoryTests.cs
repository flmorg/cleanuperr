using System.Collections.Concurrent;
using Common.Configuration.DownloadClient;
using Common.Enums;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.DownloadClient.Deluge;
using Infrastructure.Verticals.DownloadClient.Factory;
using Infrastructure.Verticals.DownloadClient.QBittorrent;
using Infrastructure.Verticals.DownloadClient.Transmission;
using NSubstitute;
using Shouldly;

namespace Infrastructure.Tests.Verticals.DownloadClient.Factory;

public class DownloadClientFactoryTests : IClassFixture<DownloadClientFactoryFixture>
{
    private readonly DownloadClientFactoryFixture _fixture;

    public DownloadClientFactoryTests(DownloadClientFactoryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Initialize_ShouldCreateClientsForEnabledConfigurations()
    {
        // Arrange
        var sut = _fixture.CreateSut();

        // Act
        await sut.Initialize();

        // Assert
        var clients = GetPrivateClientsCollection(sut);
        clients.Count.ShouldBe(3); // Only enabled clients should be initialized
        clients.Keys.ShouldContain("qbit1");
        clients.Keys.ShouldContain("transmission1");
        clients.Keys.ShouldContain("deluge1");
        clients.Keys.ShouldNotContain("disabled1");
    }

    [Fact]
    public async Task GetClient_WithExistingId_ShouldReturnExistingClient()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        await sut.Initialize();
        
        // Get an initial reference to the client
        var firstClient = sut.GetClient("qbit1");
        firstClient.ShouldNotBeNull();

        // Act
        var secondClient = sut.GetClient("qbit1");

        // Assert
        secondClient.ShouldBeSameAs(firstClient); // Should return the same instance
    }

    [Fact]
    public async Task GetClient_WithNonExistingId_ShouldCreateNewClient()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        await sut.Initialize();
        
        // Clear the internal clients collection to simulate a client that hasn't been created yet
        var clients = GetPrivateClientsCollection(sut);
        clients.Clear();

        // Act
        var client = sut.GetClient("qbit1");

        // Assert
        client.ShouldNotBeNull();
        client.ShouldBeOfType<QBitService>();
        clients.Count.ShouldBe(1);
    }

    [Fact]
    public void GetClient_WithEmptyId_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = _fixture.CreateSut();

        // Act & Assert
        Should.Throw<ArgumentException>(() => sut.GetClient(string.Empty));
    }

    [Fact]
    public async Task GetClient_WithInvalidId_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        await sut.Initialize();
        
        // Act & Assert
        Should.Throw<KeyNotFoundException>(() => sut.GetClient("invalid-client-id"));
    }

    [Fact]
    public async Task GetAllClients_ShouldReturnAllEnabledClients()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        await sut.Initialize();

        // Act
        var clients = sut.GetAllClients();

        // Assert
        clients.Count().ShouldBe(3);
        clients.Select(c => c.GetClientId()).ShouldContain("qbit1");
        clients.Select(c => c.GetClientId()).ShouldContain("transmission1");
        clients.Select(c => c.GetClientId()).ShouldContain("deluge1");
    }

    [Fact]
    public async Task GetClientByType_ShouldReturnCorrectClientType()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        await sut.Initialize();

        // Act
        var qbitClients = sut.GetClientsByType(DownloadClient.QBittorrent);
        var transmissionClients = sut.GetClientsByType(DownloadClient.Transmission);
        var delugeClients = sut.GetClientsByType(DownloadClient.Deluge);

        // Assert
        qbitClients.Count().ShouldBe(1);
        qbitClients.First().ShouldBeOfType<QBitService>();
        
        transmissionClients.Count().ShouldBe(1);
        transmissionClients.First().ShouldBeOfType<TransmissionService>();
        
        delugeClients.Count().ShouldBe(1);
        delugeClients.First().ShouldBeOfType<DelugeService>();
    }

    [Fact]
    public async Task CreateClient_WithValidConfig_ShouldReturnInitializedClient()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        var config = _fixture.DownloadClientConfig.Clients.First(c => c.Type == DownloadClient.QBittorrent);

        // Act
        var client = await sut.CreateClient(config.Id);

        // Assert
        client.ShouldNotBeNull();
        client.GetClientId().ShouldBe(config.Id);
    }

    [Fact]
    public async Task RefreshClients_ShouldReinitializeAllClients()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        await sut.Initialize();
        
        // Get initial collection of clients
        var initialClients = sut.GetAllClients().ToList();
        
        // Now modify the config to add a new client
        var updatedConfig = new DownloadClientConfig
        {
            Clients = new List<ClientConfig>(_fixture.DownloadClientConfig.Clients)
        };
        
        // Add a new client
        updatedConfig.Clients.Add(new ClientConfig
        {
            Id = "new-client",
            Name = "New QBittorrent",
            Type = DownloadClient.QBittorrent,
            Enabled = true,
            Url = "http://localhost:9999"
        });
        
        // Update the mock ConfigManager to return the updated config
        _fixture.ConfigManager.GetDownloadClientConfigAsync().Returns(Task.FromResult(updatedConfig));

        // Act
        await sut.RefreshClients();
        var refreshedClients = sut.GetAllClients().ToList();

        // Assert
        refreshedClients.Count.ShouldBe(4); // Should have one more client now
        refreshedClients.Select(c => c.GetClientId()).ShouldContain("new-client");
    }
    
    // Helper method to access the private _clients field using reflection
    private ConcurrentDictionary<string, IDownloadService> GetPrivateClientsCollection(DownloadClientFactory factory)
    {
        var field = typeof(DownloadClientFactory).GetField("_clients", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        return (ConcurrentDictionary<string, IDownloadService>)field!.GetValue(factory)!;
    }
}
