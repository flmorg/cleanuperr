using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Data.Models.Configuration.QueueCleaner;
using Common.CustomDataTypes;
using Data.Enums;
using Infrastructure.Extensions;
using Infrastructure.Verticals.Context;
using Microsoft.Extensions.Logging;
using QBittorrent.Client;

namespace Infrastructure.Verticals.DownloadClient.QBittorrent;

public partial class QBitService
{
    /// <inheritdoc/>
    public override async Task<DownloadCheckResult> ShouldRemoveFromArrQueueAsync(string hash, IReadOnlyList<string> ignoredDownloads)
    {
        DownloadCheckResult result = new();
        TorrentInfo? download = (await _client.GetTorrentListAsync(new TorrentListQuery { Hashes = [hash] }))
            .FirstOrDefault();

        if (download is null)
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
            return result;
        }

        result.Found = true;

        IReadOnlyList<TorrentTracker> trackers = await GetTrackersAsync(hash);

        if (ignoredDownloads.Count > 0 &&
            (download.ShouldIgnore(ignoredDownloads) || trackers.Any(x => x.ShouldIgnore(ignoredDownloads))))
        {
            _logger.LogInformation("skip | download is ignored | {name}", download.Name);
            return result;
        }

        TorrentProperties? torrentProperties = await _client.GetTorrentPropertiesAsync(hash);

        if (torrentProperties is null)
        {
            _logger.LogDebug("failed to find torrent properties {hash} in the download client", hash);
            return result;
        }

        result.IsPrivate = torrentProperties.AdditionalData.TryGetValue("is_private", out var dictValue) &&
                           bool.TryParse(dictValue?.ToString(), out bool boolValue)
                           && boolValue;

        IReadOnlyList<TorrentContent>? files = await _client.GetTorrentContentsAsync(hash);

        if (files?.Count is > 0 && files.All(x => x.Priority is TorrentContentPriority.Skip))
        {
            result.ShouldRemove = true;

            // if all files were blocked by qBittorrent
            if (download is { CompletionOn: not null, Downloaded: null or 0 })
            {
                result.DeleteReason = DeleteReason.AllFilesSkippedByQBit;
                return result;
            }

            // remove if all files are unwanted
            result.DeleteReason = DeleteReason.AllFilesSkipped;
            return result;
        }

        (result.ShouldRemove, result.DeleteReason) = await EvaluateDownloadRemoval(download, result.IsPrivate, files);

        return result;
    }
    
    private async Task<(bool ShouldRemove, DeleteReason Reason)> BlockUnwantedFilesAsync(
        TorrentInfo torrent,
        bool isPrivate,
        IReadOnlyList<TorrentContent>? files
    )
    {
        var queueCleanerConfig = ContextProvider.Get<QueueCleanerConfig>(nameof(QueueCleanerConfig));
        
        if (!queueCleanerConfig.ContentBlocker.Enabled)
        {
            return (false, DeleteReason.None);
        }

        if (queueCleanerConfig.ContentBlocker.IgnorePrivate && isPrivate)
        {
            // ignore private trackers
            _logger.LogDebug("skip unwanted files check | download is private | {name}", torrent.Name);
            return (false, DeleteReason.None);
        }

        if (files is null)
        {
            _logger.LogDebug("failed to find files for {name}", torrent.Name);
            return (false, DeleteReason.None);
        }

        List<int> unwantedFiles = [];
        long totalFiles = 0;
        long totalUnwantedFiles = 0;

        InstanceType instanceType = (InstanceType)ContextProvider.Get<object>(nameof(InstanceType));
        BlocklistType blocklistType = _blocklistProvider.GetBlocklistType(instanceType);
        ConcurrentBag<string> patterns = _blocklistProvider.GetPatterns(instanceType);
        ConcurrentBag<Regex> regexes = _blocklistProvider.GetRegexes(instanceType);

        foreach (TorrentContent file in files)
        {
            if (!file.Index.HasValue)
            {
                continue;
            }

            totalFiles++;

            if (file.Priority is TorrentContentPriority.Skip)
            {
                totalUnwantedFiles++;
                continue;
            }

            if (_filenameEvaluator.IsValid(file.Name, blocklistType, patterns, regexes))
            {
                continue;
            }

            _logger.LogInformation("unwanted file found | {file}", file.Name);
            unwantedFiles.Add(file.Index.Value);
            totalUnwantedFiles++;
        }

        if (unwantedFiles.Count is 0)
        {
            return (false, DeleteReason.None);
        }

        if (totalUnwantedFiles == totalFiles)
        {
            // Skip marking files as unwanted. The download will be removed completely.
            return (true, DeleteReason.AllFilesBlocked);
        }
        
        _logger.LogTrace("marking {count} unwanted files as skipped for {name}", unwantedFiles.Count, torrent.Name);

        foreach (int fileIndex in unwantedFiles)
        {
            await _dryRunInterceptor.InterceptAsync(MarkFileAsSkipped, torrent.Hash, fileIndex);
        }

        return (false, DeleteReason.None);
    }

    protected virtual async Task MarkFileAsSkipped(string hash, int fileIndex)
    {
        await _client.SetFilePriorityAsync(hash, fileIndex, TorrentContentPriority.Skip);
    }

    private async Task<(bool, DeleteReason)> EvaluateDownloadRemoval(
        TorrentInfo torrent,
        bool isPrivate,
        IReadOnlyList<TorrentContent>? files
    )
    {
        (bool ShouldRemove, DeleteReason Reason) result = await BlockUnwantedFilesAsync(torrent, isPrivate, files);

        if (result.ShouldRemove)
        {
            return result;
        }
        
        result = await CheckIfSlow(torrent, isPrivate);

        if (result.ShouldRemove)
        {
            return result;
        }

        return await CheckIfStuck(torrent, isPrivate);
    }

    private async Task<(bool ShouldRemove, DeleteReason Reason)> CheckIfSlow(TorrentInfo download, bool isPrivate)
    {
        var queueCleanerConfig = ContextProvider.Get<QueueCleanerConfig>(nameof(QueueCleanerConfig));
        
        if (queueCleanerConfig.Slow.MaxStrikes is 0)
        {
            return (false, DeleteReason.None);
        }

        if (download.State is not (TorrentState.Downloading or TorrentState.ForcedDownload))
        {
            return (false, DeleteReason.None);
        }

        if (download.DownloadSpeed <= 0)
        {
            return (false, DeleteReason.None);
        }

        if (queueCleanerConfig.Slow.IgnorePrivate && isPrivate)
        {
            // ignore private trackers
            _logger.LogDebug("skip slow check | download is private | {name}", download.Name);
            return (false, DeleteReason.None);
        }

        if (download.Size > (queueCleanerConfig.Slow.IgnoreAboveSizeByteSize?.Bytes ?? long.MaxValue))
        {
            _logger.LogDebug("skip slow check | download is too large | {name}", download.Name);
            return (false, DeleteReason.None);
        }

        ByteSize minSpeed = queueCleanerConfig.Slow.MinSpeedByteSize;
        ByteSize currentSpeed = new ByteSize(download.DownloadSpeed);
        SmartTimeSpan maxTime = SmartTimeSpan.FromHours(queueCleanerConfig.Slow.MaxTime);
        SmartTimeSpan currentTime = new SmartTimeSpan(download.EstimatedTime ?? TimeSpan.Zero);

        return await CheckIfSlow(
            download.Hash,
            download.Name,
            minSpeed,
            currentSpeed,
            maxTime,
            currentTime
        );
    }

    private async Task<(bool ShouldRemove, DeleteReason Reason)> CheckIfStuck(TorrentInfo torrent, bool isPrivate)
    {
        var queueCleanerConfig = ContextProvider.Get<QueueCleanerConfig>(nameof(QueueCleanerConfig));
        
        if (queueCleanerConfig.Stalled.MaxStrikes is 0 && queueCleanerConfig.Stalled.DownloadingMetadataMaxStrikes is 0)
        {
            return (false, DeleteReason.None);
        }

        if (torrent.State is not TorrentState.StalledDownload and not TorrentState.FetchingMetadata
            and not TorrentState.ForcedFetchingMetadata)
        {
            // ignore other states
            return (false, DeleteReason.None);
        }

        if (queueCleanerConfig.Stalled.MaxStrikes > 0 && torrent.State is TorrentState.StalledDownload)
        {
            if (queueCleanerConfig.Stalled.IgnorePrivate && isPrivate)
            {
                // ignore private trackers
                _logger.LogDebug("skip stalled check | download is private | {name}", torrent.Name);
            }
            else
            {
                ResetStalledStrikesOnProgress(torrent.Hash, torrent.Downloaded ?? 0);

                return (
                    await _striker.StrikeAndCheckLimit(torrent.Hash, torrent.Name, queueCleanerConfig.Stalled.MaxStrikes,
                        StrikeType.Stalled), DeleteReason.Stalled);
            }
        }

        if (queueCleanerConfig.Stalled.DownloadingMetadataMaxStrikes > 0 && torrent.State is not TorrentState.StalledDownload)
        {
            return (
                await _striker.StrikeAndCheckLimit(torrent.Hash, torrent.Name, queueCleanerConfig.Stalled.DownloadingMetadataMaxStrikes,
                    StrikeType.DownloadingMetadata), DeleteReason.DownloadingMetadata);
        }

        return (false, DeleteReason.None);
    }
}