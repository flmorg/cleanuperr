// using Data.Models.Configuration.ContentBlocker;
// using Data.Models.Configuration.DownloadCleaner;
// using Data.Models.Configuration.QueueCleaner;
// using Infrastructure.Interceptors;
// using Infrastructure.Verticals.ContentBlocker;
// using Infrastructure.Verticals.DownloadClient;
// using Infrastructure.Verticals.Files;
// using Infrastructure.Verticals.ItemStriker;
// using Infrastructure.Verticals.Notifications;
// using Microsoft.Extensions.Caching.Memory;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
// using NSubstitute;
//
// namespace Infrastructure.Tests.Verticals.DownloadClient;
//
// public class DownloadServiceFixture : IDisposable
// {
//     public ILogger<DownloadService> Logger { get; set; }
//     public IMemoryCache Cache { get; set; }
//     public IStriker Striker { get; set; }
//
//     public DownloadServiceFixture()
//     {
//         Logger = Substitute.For<ILogger<DownloadService>>();
//         Cache = Substitute.For<IMemoryCache>();
//         Striker = Substitute.For<IStriker>();
//     }
//
//     public TestDownloadService CreateSut(
//         QueueCleanerConfig? queueCleanerConfig = null,
//         ContentBlockerConfig? contentBlockerConfig = null
//     )
//     {
//         queueCleanerConfig ??= new QueueCleanerConfig
//         {
//             Enabled = true,
//             RunSequentially = true,
//             StalledResetStrikesOnProgress = true,
//             StalledMaxStrikes = 3
//         };
//
//         var queueCleanerOptions = Substitute.For<IOptions<QueueCleanerConfig>>();
//         queueCleanerOptions.Value.Returns(queueCleanerConfig);
//
//         contentBlockerConfig ??= new ContentBlockerConfig
//         {
//             Enabled = true
//         };
//         
//         var contentBlockerOptions = Substitute.For<IOptions<ContentBlockerConfig>>();
//         contentBlockerOptions.Value.Returns(contentBlockerConfig);
//
//         var downloadCleanerOptions = Substitute.For<IOptions<DownloadCleanerConfig>>();
//         downloadCleanerOptions.Value.Returns(new DownloadCleanerConfig());
//
//         var filenameEvaluator = Substitute.For<IFilenameEvaluator>();
//         var notifier = Substitute.For<INotificationPublisher>();
//         var dryRunInterceptor = Substitute.For<IDryRunInterceptor>();
//         var hardlinkFileService = Substitute.For<IHardLinkFileService>();
//
//         return new TestDownloadService(
//             Logger,
//             queueCleanerOptions,
//             contentBlockerOptions,
//             downloadCleanerOptions,
//             Cache,
//             filenameEvaluator,
//             Striker,
//             notifier,
//             dryRunInterceptor,
//             hardlinkFileService
//         );
//     }
//
//     public void Dispose()
//     {
//         // Cleanup if needed
//     }
// }