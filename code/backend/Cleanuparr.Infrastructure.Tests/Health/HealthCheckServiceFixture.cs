// using Common.Configuration;
// using Common.Enums;
// using Infrastructure.Configuration;
// using Infrastructure.Health;
// using Infrastructure.Verticals.DownloadClient;
// using Infrastructure.Verticals.DownloadClient.Factory;
// using Microsoft.Extensions.Logging;
// using NSubstitute;
// using NSubstitute.ExceptionExtensions;
//
// namespace Infrastructure.Tests.Health;
//
// public class HealthCheckServiceFixture : IDisposable
// {
//     public ILogger<HealthCheckService> Logger { get; }
//     public IConfigManager ConfigManager { get; }
//     public IDownloadClientFactory ClientFactory { get; }
//     public IDownloadService MockClient { get; }
//     public DownloadClientConfigs DownloadClientConfigs { get; }
//
//     public HealthCheckServiceFixture()
//     {
//         Logger = Substitute.For<ILogger<HealthCheckService>>();
//         ConfigManager = Substitute.For<IConfigManager>();
//         ClientFactory = Substitute.For<IDownloadClientFactory>();
//         MockClient = Substitute.For<IDownloadService>();
//         Guid clientId = Guid.NewGuid();
//         
//         // Set up test download client config
//         DownloadClientConfigs = new DownloadClientConfigs
//         {
//             Clients = new List<DownloadClientConfig>
//             {
//                 new()
//                 {
//                     Id = clientId,
//                     Name = "Test QBittorrent",
//                     Type = DownloadClientType.QBittorrent,
//                     Enabled = true,
//                     Username = "admin",
//                     Password = "adminadmin"
//                 },
//                 new()
//                 {
//                     Id = Guid.NewGuid(),
//                     Name = "Test Transmission",
//                     Type = DownloadClientType.Transmission,
//                     Enabled = true,
//                     Username = "admin",
//                     Password = "adminadmin"
//                 },
//                 new()
//                 {
//                     Id = Guid.NewGuid(),
//                     Name = "Disabled Client",
//                     Type = DownloadClientType.QBittorrent,
//                     Enabled = false,
//                 }
//             }
//         };
//         
//         // Set up the mock client factory
//         ClientFactory.GetClient(Arg.Any<Guid>()).Returns(MockClient);
//         MockClient.GetClientId().Returns(clientId);
//         
//         // Set up mock config manager
//         ConfigManager.GetConfiguration<DownloadClientConfigs>().Returns(DownloadClientConfigs);
//     }
//
//     public HealthCheckService CreateSut()
//     {
//         return new HealthCheckService(Logger, ConfigManager, ClientFactory);
//     }
//     
//     public void SetupHealthyClient(Guid clientId)
//     {
//         // Setup a client that will successfully login
//         MockClient.LoginAsync().Returns(Task.CompletedTask);
//     }
//     
//     public void SetupUnhealthyClient(Guid clientId, string errorMessage = "Failed to connect")
//     {
//         // Setup a client that will fail to login
//         MockClient.LoginAsync().Throws(new Exception(errorMessage));
//     }
//
//     public void Dispose()
//     {
//         // Cleanup if needed
//     }
// }
