// using Infrastructure.Health;
// using NSubstitute;
// using Shouldly;
//
// namespace Infrastructure.Tests.Health;
//
// public class HealthCheckServiceTests : IClassFixture<HealthCheckServiceFixture>
// {
//     private readonly HealthCheckServiceFixture _fixture;
//
//     public HealthCheckServiceTests(HealthCheckServiceFixture fixture)
//     {
//         _fixture = fixture;
//     }
//
//     [Fact]
//     public async Task CheckClientHealthAsync_WithHealthyClient_ShouldReturnHealthyStatus()
//     {
//         // Arrange
//         var sut = _fixture.CreateSut();
//         _fixture.SetupHealthyClient(new Guid("00000000-0000-0000-0000-000000000001"));
//         
//         // Act
//         var result = await sut.CheckClientHealthAsync(new Guid("00000000-0000-0000-0000-000000000001"));
//         
//         // Assert
//         result.ShouldSatisfyAllConditions(
//             () => result.IsHealthy.ShouldBeTrue(),
//             () => result.ClientId.ShouldBe(new Guid("00000000-0000-0000-0000-000000000001")),
//             () => result.ErrorMessage.ShouldBeNull(),
//             () => result.LastChecked.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow)
//         );
//     }
//     
//     [Fact]
//     public async Task CheckClientHealthAsync_WithUnhealthyClient_ShouldReturnUnhealthyStatus()
//     {
//         // Arrange
//         var sut = _fixture.CreateSut();
//         _fixture.SetupUnhealthyClient(new Guid("00000000-0000-0000-0000-000000000001"), "Connection refused");
//         
//         // Act
//         var result = await sut.CheckClientHealthAsync(new Guid("00000000-0000-0000-0000-000000000001"));
//         
//         // Assert
//         result.ShouldSatisfyAllConditions(
//             () => result.IsHealthy.ShouldBeFalse(),
//             () => result.ClientId.ShouldBe(new Guid("00000000-0000-0000-0000-000000000001")),
//             () => result.ErrorMessage?.ShouldContain("Connection refused"),
//             () => result.LastChecked.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow)
//         );
//     }
//     
//     [Fact]
//     public async Task CheckClientHealthAsync_WithNonExistentClient_ShouldReturnErrorStatus()
//     {
//         // Arrange
//         var sut = _fixture.CreateSut();
//         
//         // Configure the ConfigManager to return null for the client config
//         _fixture.ConfigManager.GetConfigurationAsync<DownloadClientConfigs>().Returns(
//             Task.FromResult<DownloadClientConfigs>(new())
//         );
//         
//         // Act
//         var result = await sut.CheckClientHealthAsync(new Guid("00000000-0000-0000-0000-000000000010"));
//         
//         // Assert
//         result.ShouldSatisfyAllConditions(
//             () => result.IsHealthy.ShouldBeFalse(),
//             () => result.ClientId.ShouldBe(new Guid("00000000-0000-0000-0000-000000000010")),
//             () => result.ErrorMessage?.ShouldContain("not found"),
//             () => result.LastChecked.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow)
//         );
//     }
//     
//     [Fact]
//     public async Task CheckAllClientsHealthAsync_ShouldReturnAllEnabledClients()
//     {
//         // Arrange
//         var sut = _fixture.CreateSut();
//         _fixture.SetupHealthyClient(new Guid("00000000-0000-0000-0000-000000000001"));
//         _fixture.SetupUnhealthyClient(new Guid("00000000-0000-0000-0000-000000000002"));
//         
//         // Act
//         var results = await sut.CheckAllClientsHealthAsync();
//         
//         // Assert
//         results.Count.ShouldBe(2); // Only enabled clients
//         results.Keys.ShouldContain(new Guid("00000000-0000-0000-0000-000000000001"));
//         results.Keys.ShouldContain(new Guid("00000000-0000-0000-0000-000000000002"));
//         results[new Guid("00000000-0000-0000-0000-000000000001")].IsHealthy.ShouldBeTrue();
//         results[new Guid("00000000-0000-0000-0000-000000000002")].IsHealthy.ShouldBeFalse();
//     }
//     
//     [Fact]
//     public async Task ClientHealthChanged_ShouldRaiseEventOnHealthStateChange()
//     {
//         // Arrange
//         var sut = _fixture.CreateSut();
//         _fixture.SetupHealthyClient(new Guid("00000000-0000-0000-0000-000000000001"));
//         
//         ClientHealthChangedEventArgs? capturedArgs = null;
//         sut.ClientHealthChanged += (_, args) => capturedArgs = args;
//         
//         // Act - first check establishes initial state
//         var firstResult = await sut.CheckClientHealthAsync(new Guid("00000000-0000-0000-0000-000000000001"));
//         
//         // Setup client to be unhealthy for second check
//         _fixture.SetupUnhealthyClient(new Guid("00000000-0000-0000-0000-000000000001"));
//         
//         // Act - second check changes state
//         var secondResult = await sut.CheckClientHealthAsync(new Guid("00000000-0000-0000-0000-000000000001"));
//         
//         // Assert
//         capturedArgs.ShouldNotBeNull();
//         capturedArgs.ClientId.ShouldBe(new Guid("00000000-0000-0000-0000-000000000001"));
//         capturedArgs.Status.IsHealthy.ShouldBeFalse();
//         capturedArgs.IsDegraded.ShouldBeTrue();
//         capturedArgs.IsRecovered.ShouldBeFalse();
//     }
//     
//     [Fact]
//     public async Task GetClientHealth_ShouldReturnCachedStatus()
//     {
//         // Arrange
//         var sut = _fixture.CreateSut();
//         _fixture.SetupHealthyClient(new Guid("00000000-0000-0000-0000-000000000001"));
//         
//         // Perform a check to cache the status
//         await sut.CheckClientHealthAsync(new Guid("00000000-0000-0000-0000-000000000001"));
//         
//         // Act
//         var result = sut.GetClientHealth(new Guid("00000000-0000-0000-0000-000000000001"));
//         
//         // Assert
//         result.ShouldNotBeNull();
//         result.IsHealthy.ShouldBeTrue();
//         result.ClientId.ShouldBe(new Guid("00000000-0000-0000-0000-000000000001"));
//     }
//     
//     [Fact]
//     public void GetClientHealth_WithNoCheck_ShouldReturnNull()
//     {
//         // Arrange
//         var sut = _fixture.CreateSut();
//         
//         // Act
//         var result = sut.GetClientHealth(new Guid("00000000-0000-0000-0000-000000000001"));
//         
//         // Assert
//         result.ShouldBeNull();
//     }
//     
//     [Fact]
//     public async Task GetAllClientHealth_ShouldReturnAllCheckedClients()
//     {
//         // Arrange
//         var sut = _fixture.CreateSut();
//         _fixture.SetupHealthyClient(new Guid("00000000-0000-0000-0000-000000000001"));
//         _fixture.SetupUnhealthyClient(new Guid("00000000-0000-0000-0000-000000000002"));
//         
//         // Perform checks to cache statuses
//         await sut.CheckClientHealthAsync(new Guid("00000000-0000-0000-0000-000000000001"));
//         await sut.CheckClientHealthAsync(new Guid("00000000-0000-0000-0000-000000000002"));
//         
//         // Act
//         var results = sut.GetAllClientHealth();
//         
//         // Assert
//         results.Count.ShouldBe(2);
//         results.Keys.ShouldContain(new Guid("00000000-0000-0000-0000-000000000001"));
//         results.Keys.ShouldContain(new Guid("00000000-0000-0000-0000-000000000002"));
//         results[new Guid("00000000-0000-0000-0000-000000000001")].IsHealthy.ShouldBeTrue();
//         results[new Guid("00000000-0000-0000-0000-000000000002")].IsHealthy.ShouldBeFalse();
//     }
// }
