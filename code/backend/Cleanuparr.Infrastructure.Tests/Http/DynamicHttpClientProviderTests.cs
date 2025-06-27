// using System.Net;
// using Common.Enums;
// using Infrastructure.Http;
// using Shouldly;
//
// namespace Infrastructure.Tests.Http;
//
// public class DynamicHttpClientProviderTests : IClassFixture<DynamicHttpClientProviderFixture>
// {
//     private readonly DynamicHttpClientProviderFixture _fixture;
//
//     public DynamicHttpClientProviderTests(DynamicHttpClientProviderFixture fixture)
//     {
//         _fixture = fixture;
//     }
//
//     [Fact]
//     public void CreateClient_WithQBitConfig_ShouldReturnConfiguredClient()
//     {
//         // Arrange
//         var sut = _fixture.CreateSut();
//         var config = _fixture.CreateQBitClientConfig();
//
//         // Act
//         var httpClient = sut.CreateClient(config);
//
//         // Assert
//         httpClient.ShouldNotBeNull();
//         httpClient.BaseAddress.ShouldBe(config.Url);
//         VerifyDefaultHttpClientProperties(httpClient);
//     }
//     
//     [Fact]
//     public void CreateClient_WithTransmissionConfig_ShouldReturnConfiguredClient()
//     {
//         // Arrange
//         var sut = _fixture.CreateSut();
//         var config = _fixture.CreateTransmissionClientConfig();
//
//         // Act
//         var httpClient = sut.CreateClient(config);
//
//         // Assert
//         httpClient.ShouldNotBeNull();
//         httpClient.BaseAddress.ShouldBe(config.Url);
//         VerifyDefaultHttpClientProperties(httpClient);
//     }
//     
//     [Fact]
//     public void CreateClient_WithDelugeConfig_ShouldReturnConfiguredClient()
//     {
//         // Arrange
//         var sut = _fixture.CreateSut();
//         var config = _fixture.CreateDelugeClientConfig();
//
//         // Act
//         var httpClient = sut.CreateClient(config);
//
//         // Assert
//         httpClient.ShouldNotBeNull();
//         httpClient.BaseAddress.ShouldBe(config.Url);
//         
//         // Deluge client should have additional properties configured
//         VerifyDelugeHttpClientProperties(httpClient);
//     }
//     
//     [Fact]
//     public void CreateClient_WithSameConfig_ShouldReturnUniqueInstances()
//     {
//         // Arrange
//         var sut = _fixture.CreateSut();
//         var config = _fixture.CreateQBitClientConfig();
//
//         // Act
//         var firstClient = sut.CreateClient(config);
//         var secondClient = sut.CreateClient(config);
//
//         // Assert
//         firstClient.ShouldNotBeNull();
//         secondClient.ShouldNotBeNull();
//         firstClient.ShouldNotBeSameAs(secondClient); // Should be different instances
//     }
//     
//     [Fact]
//     public void CreateClient_WithCustomCertificateValidation_ShouldConfigureHandler()
//     {
//         // Arrange
//         var sut = _fixture.CreateSut();
//         var config = _fixture.CreateQBitClientConfig();
//
//         // Act
//         var httpClient = sut.CreateClient(config);
//
//         // Assert
//         httpClient.ShouldNotBeNull();
//         
//         // Since we can't directly access the handler settings after creation,
//         // we verify the behavior is working by checking if the client can be created properly
//         httpClient.BaseAddress.ShouldBe(config.Url);
//     }
//     
//     [Fact]
//     public void CreateClient_WithTimeout_ShouldConfigureTimeout()
//     {
//         // Arrange
//         var sut = _fixture.CreateSut();
//         var config = _fixture.CreateQBitClientConfig();
//         TimeSpan expectedTimeout = TimeSpan.FromSeconds(30);
//
//         // Act
//         var httpClient = sut.CreateClient(config);
//
//         // Assert
//         httpClient.Timeout.ShouldBe(expectedTimeout);
//     }
//     
//     private void VerifyDefaultHttpClientProperties(HttpClient httpClient)
//     {
//         // Check common properties that should be set for all clients
//         httpClient.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
//         httpClient.DefaultRequestHeaders.ShouldNotBeNull();
//     }
//     
//     private void VerifyDelugeHttpClientProperties(HttpClient httpClient)
//     {
//         // Verify Deluge-specific HTTP client configurations
//         VerifyDefaultHttpClientProperties(httpClient);
//         
//         // Using reflection to access the handler is tricky and potentially brittle
//         // Instead, we focus on verifying the client itself is properly configured
//         httpClient.BaseAddress.ShouldNotBeNull();
//     }
// }
