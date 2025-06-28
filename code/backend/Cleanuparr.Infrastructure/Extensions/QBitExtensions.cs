using Cleanuparr.Infrastructure.Services;
using QBittorrent.Client;

namespace Cleanuparr.Infrastructure.Extensions;

public static class QBitExtensions
{
    public static bool ShouldIgnore(this TorrentInfo download, IReadOnlyList<string> ignoredDownloads)
    {
        foreach (string value in ignoredDownloads)
        {
            if (download.Hash.Equals(value, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
            
            if (download.Category.Equals(value, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (download.Tags.Contains(value, StringComparer.InvariantCultureIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool ShouldIgnore(this TorrentTracker tracker, IReadOnlyList<string> ignoredDownloads)
    {
        string? trackerUrl = UriService.GetDomain(tracker.Url);

        if (trackerUrl is null)
        {
            return false;
        }
        
        foreach (string value in ignoredDownloads)
        {
            if (trackerUrl.EndsWith(value, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}