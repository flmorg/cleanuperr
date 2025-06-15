using Common.Attributes;
using Data.Models.Configuration.DownloadCleaner;
using Data.Enums;
using Data.Models.Deluge.Response;
using Infrastructure.Extensions;
using Infrastructure.Verticals.Context;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.DownloadClient.Deluge;

public partial class DelugeService
{
    public override async Task<List<object>?> GetSeedingDownloads()
    {
        return (await _client.GetStatusForAllTorrents())
            ?.Where(x => !string.IsNullOrEmpty(x.Hash))
            .Where(x => x.State?.Equals("seeding", StringComparison.InvariantCultureIgnoreCase) is true)
            .Cast<object>()
            .ToList();
    }

    public override List<object>? FilterDownloadsToBeCleanedAsync(List<object>? downloads, List<CleanCategory> categories) =>
        downloads
            ?.Cast<DownloadStatus>()
            .Where(x => categories.Any(cat => cat.Name.Equals(x.Label, StringComparison.InvariantCultureIgnoreCase)))
            .Cast<object>()
            .ToList();

    public override List<object>? FilterDownloadsToChangeCategoryAsync(List<object>? downloads, List<string> categories) =>
        downloads
            ?.Cast<DownloadStatus>()
            .Where(x => !string.IsNullOrEmpty(x.Hash))
            .Where(x => categories.Any(cat => cat.Equals(x.Label, StringComparison.InvariantCultureIgnoreCase)))
            .Cast<object>()
            .ToList();

    /// <inheritdoc/>
    public override async Task CleanDownloadsAsync(List<object>? downloads, List<CleanCategory> categoriesToClean, HashSet<string> excludedHashes,
        IReadOnlyList<string> ignoredDownloads)
    {
        if (downloads?.Count is null or 0)
        {
            return;
        }
        
        foreach (DownloadStatus download in downloads)
        {
            if (string.IsNullOrEmpty(download.Hash))
            {
                continue;
            }
            
            if (excludedHashes.Any(x => x.Equals(download.Hash, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogDebug("skip | download is used by an arr | {name}", download.Name);
                continue;
            }

            if (ignoredDownloads.Count > 0 && download.ShouldIgnore(ignoredDownloads))
            {
                _logger.LogInformation("skip | download is ignored | {name}", download.Name);
                continue;
            }
            
            CleanCategory? category = categoriesToClean
                .FirstOrDefault(x => x.Name.Equals(download.Label, StringComparison.InvariantCultureIgnoreCase));

            if (category is null)
            {
                continue;
            }

            if (!_downloadCleanerConfig.DeletePrivate && download.Private)
            {
                _logger.LogDebug("skip | download is private | {name}", download.Name);
                continue;
            }
            
            ContextProvider.Set("downloadName", download.Name);
            ContextProvider.Set("hash", download.Hash);
            
            TimeSpan seedingTime = TimeSpan.FromSeconds(download.SeedingTime);
            SeedingCheckResult result = ShouldCleanDownload(download.Ratio, seedingTime, category);

            if (!result.ShouldClean)
            {
                continue;
            }

            await _dryRunInterceptor.InterceptAsync(DeleteDownload, download.Hash);

            _logger.LogInformation(
                "download cleaned | {reason} reached | {name}",
                result.Reason is CleanReason.MaxRatioReached
                    ? "MAX_RATIO & MIN_SEED_TIME"
                    : "MAX_SEED_TIME",
                download.Name
            );
            
            await _eventPublisher.PublishDownloadCleaned(download.Ratio, seedingTime, category.Name, result.Reason);
        }
    }

    public override async Task CreateCategoryAsync(string name)
    {
        IReadOnlyList<string> existingLabels = await _client.GetLabels();

        if (existingLabels.Contains(name, StringComparer.InvariantCultureIgnoreCase))
        {
            return;
        }
        
        await _dryRunInterceptor.InterceptAsync(CreateLabel, name);
    }

    public override async Task ChangeCategoryForNoHardLinksAsync(List<object>? downloads, HashSet<string> excludedHashes, IReadOnlyList<string> ignoredDownloads)
    {
        if (downloads?.Count is null or 0)
        {
            return;
        }

        if (!string.IsNullOrEmpty(_downloadCleanerConfig.UnlinkedIgnoredRootDir))
        {
            _hardLinkFileService.PopulateFileCounts(_downloadCleanerConfig.UnlinkedIgnoredRootDir);
        }
        
        foreach (DownloadStatus download in downloads.Cast<DownloadStatus>())
        {
            if (string.IsNullOrEmpty(download.Hash) || string.IsNullOrEmpty(download.Name) || string.IsNullOrEmpty(download.Label))
            {
                continue;
            }
            
            if (excludedHashes.Any(x => x.Equals(download.Hash, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogDebug("skip | download is used by an arr | {name}", download.Name);
                continue;
            }

            if (ignoredDownloads.Count > 0 && download.ShouldIgnore(ignoredDownloads))
            {
                _logger.LogInformation("skip | download is ignored | {name}", download.Name);
                continue;
            }
        
            ContextProvider.Set("downloadName", download.Name);
            ContextProvider.Set("hash", download.Hash);
            
            DelugeContents? contents = null;
            try
            {
                contents = await _client.GetTorrentFiles(download.Hash);
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "failed to find torrent files for {name}", download.Name);
                continue;
            }
            
            bool hasHardlinks = false;
            
            ProcessFiles(contents?.Contents, (_, file) =>
            {
                string filePath = string.Join(Path.DirectorySeparatorChar, Path.Combine(download.DownloadLocation, file.Path).Split(['\\', '/']));

                if (file.Priority <= 0)
                {
                    _logger.LogDebug("skip | file is not downloaded | {file}", filePath);
                    return;
                }
                
                long hardlinkCount = _hardLinkFileService
                    .GetHardLinkCount(filePath, !string.IsNullOrEmpty(_downloadCleanerConfig.UnlinkedIgnoredRootDir));
        
                if (hardlinkCount < 0)
                {
                    _logger.LogDebug("skip | could not get file properties | {file}", filePath);
                    hasHardlinks = true;
                    return;
                }
        
                if (hardlinkCount > 0)
                {
                    hasHardlinks = true;
                }
            });
            
            if (hasHardlinks)
            {
                _logger.LogDebug("skip | download has hardlinks | {name}", download.Name);
                continue;
            }
            
            await _dryRunInterceptor.InterceptAsync(ChangeLabel, download.Hash, _downloadCleanerConfig.UnlinkedTargetCategory);
            
            _logger.LogInformation("category changed for {name}", download.Name);
            
            await _eventPublisher.PublishCategoryChanged(download.Label, _downloadCleanerConfig.UnlinkedTargetCategory);
            
            download.Label = _downloadCleanerConfig.UnlinkedTargetCategory;
        }
    }

    /// <inheritdoc/>
    [DryRunSafeguard]
    public override async Task DeleteDownload(string hash)
    {
        hash = hash.ToLowerInvariant();
        
        await _client.DeleteTorrents([hash]);
    }

    [DryRunSafeguard]
    protected async Task CreateLabel(string name)
    {
        await _client.CreateLabel(name);
    }
    
    [DryRunSafeguard]
    protected virtual async Task ChangeLabel(string hash, string newLabel)
    {
        await _client.SetTorrentLabel(hash, newLabel);
    }
}