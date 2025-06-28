using Cleanuparr.Infrastructure.Services;
using Transmission.API.RPC.Entity;

namespace Cleanuparr.Infrastructure.Extensions;

public static class TransmissionExtensions
{
    public static bool ShouldIgnore(this TorrentInfo download, IReadOnlyList<string> ignoredDownloads)
    {
        foreach (string value in ignoredDownloads)
        {
            if (download.HashString?.Equals(value, StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                return true;
            }

            if (download.GetCategory().Equals(value, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            bool? hasIgnoredTracker = download.Trackers?
                .Any(x => UriService.GetDomain(x.Announce)?.EndsWith(value, StringComparison.InvariantCultureIgnoreCase) ?? false);
            
            if (hasIgnoredTracker is true)
            {
                return true;
            }
        }

        return false;
    }

    public static string GetCategory(this TorrentInfo download)
    {
        if (string.IsNullOrEmpty(download.DownloadDir))
        {
            return string.Empty;
        }

        return Path.GetFileName(Path.TrimEndingDirectorySeparator(download.DownloadDir));
    }
}