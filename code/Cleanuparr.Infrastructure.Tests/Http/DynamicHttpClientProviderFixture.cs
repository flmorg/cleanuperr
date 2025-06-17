// using Common.Configuration;
// using Common.Enums;
// using Infrastructure.Configuration;
// using Infrastructure.Http;
// using Infrastructure.Services;
// using Microsoft.Extensions.Logging;
// using NSubstitute;
//
// namespace Infrastructure.Tests.Http;
//
// public class DynamicHttpClientProviderFixture : IDisposable
// {
//     public ILogger<DynamicHttpClientProvider> Logger { get; }
//     
//     public DynamicHttpClientProviderFixture()
//     {
//         Logger = Substitute.For<ILogger<DynamicHttpClientProvider>>();
//     }
//     
//     public DynamicHttpClientProvider CreateSut()
//     {
//         var httpClientFactory = Substitute.For<IHttpClientFactory>();
//         var configManager = Substitute.For<IConfigManager>();
//         var certificateValidationService = Substitute.For<CertificateValidationService>();
//
//         return new DynamicHttpClientProvider(
//             Logger,
//             httpClientFactory,
//             configManager,
//             certificateValidationService);
//     }
//     
//     public DownloadClientConfig CreateQBitClientConfig()
//     {
//         return new DownloadClientConfig
//         {
//             Id = Guid.NewGuid(),
//             Name = "QBit Test",
//             Type = DownloadClientType.QBittorrent,
//             Enabled = true,
//             Host = new("http://localhost:8080"),
//             Username = "admin",
//             Password = "adminadmin"
//         };
//     }
//     
//     public DownloadClientConfig CreateTransmissionClientConfig()
//     {
//         return new DownloadClientConfig
//         {
//             Id = Guid.NewGuid(),
//             Name = "Transmission Test",
//             Type = DownloadClientType.Transmission,
//             Enabled = true,
//             Host = new("http://localhost:9091"),
//             Username = "admin",
//             Password = "adminadmin",
//             UrlBase = "transmission"
//         };
//     }
//     
//     public DownloadClientConfig CreateDelugeClientConfig()
//     {
//         return new DownloadClientConfig
//         {
//             Id = Guid.NewGuid(),
//             Name = "Deluge Test",
//             Type = DownloadClientType.Deluge,
//             Enabled = true,
//             Host = new("http://localhost:8112"),
//             Username = "admin",
//             Password = "deluge"
//         };
//     }
//     
//     public void Dispose()
//     {
//         // Cleanup if needed
//     }
// }
