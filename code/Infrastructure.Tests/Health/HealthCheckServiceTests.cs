using Infrastructure.Health;
using NSubstitute;
using Shouldly;

namespace Infrastructure.Tests.Health;

public class HealthCheckServiceTests : IClassFixture<HealthCheckServiceFixture>
{
    private readonly HealthCheckServiceFixture _fixture;

    public HealthCheckServiceTests(HealthCheckServiceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CheckClientHealthAsync_WithHealthyClient_ShouldReturnHealthyStatus()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        _fixture.SetupHealthyClient("qbit1");
        
        // Act
        var result = await sut.CheckClientHealthAsync("qbit1");
        
        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsHealthy.ShouldBeTrue(),
            () => result.ClientId.ShouldBe("qbit1"),
            () => result.ErrorMessage.ShouldBeNull(),
            () => result.LastChecked.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow)
        );
    }
    
    [Fact]
    public async Task CheckClientHealthAsync_WithUnhealthyClient_ShouldReturnUnhealthyStatus()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        _fixture.SetupUnhealthyClient("qbit1", "Connection refused");
        
        // Act
        var result = await sut.CheckClientHealthAsync("qbit1");
        
        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsHealthy.ShouldBeFalse(),
            () => result.ClientId.ShouldBe("qbit1"),
            () => result.ErrorMessage.ShouldContain("Connection refused"),
            () => result.LastChecked.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow)
        );
    }
    
    [Fact]
    public async Task CheckClientHealthAsync_WithNonExistentClient_ShouldReturnErrorStatus()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        
        // Configure the ConfigManager to return null for the client config
        _fixture.ConfigManager.GetDownloadClientConfigAsync().Returns(
            Task.FromResult<DownloadClientConfig>(null)
        );
        
        // Act
        var result = await sut.CheckClientHealthAsync("non-existent");
        
        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsHealthy.ShouldBeFalse(),
            () => result.ClientId.ShouldBe("non-existent"),
            () => result.ErrorMessage.ShouldContain("not found"),
            () => result.LastChecked.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow)
        );
    }
    
    [Fact]
    public async Task CheckAllClientsHealthAsync_ShouldReturnAllEnabledClients()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        _fixture.SetupHealthyClient("qbit1");
        _fixture.SetupUnhealthyClient("transmission1");
        
        // Act
        var results = await sut.CheckAllClientsHealthAsync();
        
        // Assert
        results.Count.ShouldBe(2); // Only enabled clients
        results.Keys.ShouldContain("qbit1");
        results.Keys.ShouldContain("transmission1");
        results["qbit1"].IsHealthy.ShouldBeTrue();
        results["transmission1"].IsHealthy.ShouldBeFalse();
    }
    
    [Fact]
    public async Task ClientHealthChanged_ShouldRaiseEventOnHealthStateChange()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        _fixture.SetupHealthyClient("qbit1");
        
        ClientHealthChangedEventArgs? capturedArgs = null;
        sut.ClientHealthChanged += (_, args) => capturedArgs = args;
        
        // Act - first check establishes initial state
        var firstResult = await sut.CheckClientHealthAsync("qbit1");
        
        // Setup client to be unhealthy for second check
        _fixture.SetupUnhealthyClient("qbit1");
        
        // Act - second check changes state
        var secondResult = await sut.CheckClientHealthAsync("qbit1");
        
        // Assert
        capturedArgs.ShouldNotBeNull();
        capturedArgs.ClientId.ShouldBe("qbit1");
        capturedArgs.Status.IsHealthy.ShouldBeFalse();
        capturedArgs.IsDegraded.ShouldBeTrue();
        capturedArgs.IsRecovered.ShouldBeFalse();
    }
    
    [Fact]
    public async Task GetClientHealth_ShouldReturnCachedStatus()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        _fixture.SetupHealthyClient("qbit1");
        
        // Perform a check to cache the status
        await sut.CheckClientHealthAsync("qbit1");
        
        // Act
        var result = sut.GetClientHealth("qbit1");
        
        // Assert
        result.ShouldNotBeNull();
        result.IsHealthy.ShouldBeTrue();
        result.ClientId.ShouldBe("qbit1");
    }
    
    [Fact]
    public void GetClientHealth_WithNoCheck_ShouldReturnNull()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        
        // Act
        var result = sut.GetClientHealth("qbit1");
        
        // Assert
        result.ShouldBeNull();
    }
    
    [Fact]
    public async Task GetAllClientHealth_ShouldReturnAllCheckedClients()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        _fixture.SetupHealthyClient("qbit1");
        _fixture.SetupUnhealthyClient("transmission1");
        
        // Perform checks to cache statuses
        await sut.CheckClientHealthAsync("qbit1");
        await sut.CheckClientHealthAsync("transmission1");
        
        // Act
        var results = sut.GetAllClientHealth();
        
        // Assert
        results.Count.ShouldBe(2);
        results.Keys.ShouldContain("qbit1");
        results.Keys.ShouldContain("transmission1");
        results["qbit1"].IsHealthy.ShouldBeTrue();
        results["transmission1"].IsHealthy.ShouldBeFalse();
    }
}
