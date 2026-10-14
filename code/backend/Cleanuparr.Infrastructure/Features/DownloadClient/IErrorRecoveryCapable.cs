using Cleanuparr.Domain.Entities;

namespace Cleanuparr.Infrastructure.Features.DownloadClient;

/// <summary>
/// Torrent clients that report an error state and can be told to resume a download.
/// rTorrent does not report one, so it is excluded.
/// </summary>
public interface IErrorRecoveryCapable : IDownloadService
{
    /// <summary>
    /// Asks the download client to resume the download.
    /// </summary>
    /// <param name="item">The download to resume.</param>
    Task ResumeDownload(IDownloadItem item);
}
