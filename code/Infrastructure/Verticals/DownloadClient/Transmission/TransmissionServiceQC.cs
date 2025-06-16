using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Attributes;
using Data.Models.Configuration.QueueCleaner;
using Common.CustomDataTypes;
using Data.Enums;
using Infrastructure.Extensions;
using Infrastructure.Verticals.Context;
using Microsoft.Extensions.Logging;
using Transmission.API.RPC.Arguments;
using Transmission.API.RPC.Entity;

namespace Infrastructure.Verticals.DownloadClient.Transmission;

public partial class TransmissionService
{
    /// <inheritdoc/>
    public override async Task<DownloadCheckResult> ShouldRemoveFromArrQueueAsync(string hash,
        IReadOnlyList<string> ignoredDownloads)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("Transmission client is not initialized");
        }
        
        DownloadCheckResult result = new();
        TorrentInfo? download = await GetTorrentAsync(hash);

        if (download is null)
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
            return result;
        }
        
        result.Found = true;
        
        if (ignoredDownloads.Count > 0 && download.ShouldIgnore(ignoredDownloads))
        {
            _logger.LogDebug("skip | download is ignored | {name}", download.Name);
            return result;
        }
        
        bool shouldRemove = download.FileStats?.Length > 0;
        bool isPrivate = download.IsPrivate ?? false;
        result.IsPrivate = isPrivate;

        foreach (TransmissionTorrentFileStats stats in download.FileStats ?? [])
        {
            if (!stats.Wanted.HasValue)
            {
                // if any files stats are missing, do not remove
                shouldRemove = false;
            }
            
            if (stats.Wanted.HasValue && stats.Wanted.Value)
            {
                // if any files are wanted, do not remove
                shouldRemove = false;
            }
        }
        
        if (shouldRemove)
        {
            // remove if all files are unwanted
            result.ShouldRemove = true;
            result.DeleteReason = DeleteReason.AllFilesSkipped;
            return result;
        }

        // remove if download is stuck
        (result.ShouldRemove, result.DeleteReason) = await EvaluateDownloadRemoval(download, isPrivate);

        return result;
    }

    private async Task<(bool ShouldRemove, DeleteReason Reason)> BlockUnwantedFilesAsync(
        TorrentInfo download,
        bool isPrivate
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
            _logger.LogDebug("skip unwanted files check | download is private | {name}", download.Name);
            return (false, DeleteReason.None);
        }
        
        if (download.Files is null)
        {
            _logger.LogDebug("failed to find files for {name}", download.Name);
            return (false, DeleteReason.None);
        }
        
        InstanceType instanceType = (InstanceType)ContextProvider.Get<object>(nameof(InstanceType));
        BlocklistType blocklistType = _blocklistProvider.GetBlocklistType(instanceType);
        ConcurrentBag<string> patterns = _blocklistProvider.GetPatterns(instanceType);
        ConcurrentBag<Regex> regexes = _blocklistProvider.GetRegexes(instanceType);
        
        List<long> unwantedFiles = [];
        long totalFiles = 0;
        long totalUnwantedFiles = 0;
        
        for (int i = 0; i < download.Files.Length; i++)
        {
            if (download.FileStats?[i].Wanted == null)
            {
                continue;
            }

            totalFiles++;
            
            if (!download.FileStats[i].Wanted.Value)
            {
                totalUnwantedFiles++;
                continue;
            }

            if (_filenameEvaluator.IsValid(download.Files[i].Name, blocklistType, patterns, regexes))
            {
                continue;
            }
            
            _logger.LogInformation("unwanted file found | {file}", download.Files[i].Name);
            unwantedFiles.Add(i);
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
        
        _logger.LogTrace("marking {count} unwanted files as skipped for {name}", totalUnwantedFiles, download.Name);

        await _dryRunInterceptor.InterceptAsync(SetUnwantedFiles, download.Id, unwantedFiles.ToArray());

        return (false, DeleteReason.None);
    }
    
    [DryRunSafeguard]
    protected virtual async Task SetUnwantedFiles(long downloadId, long[] unwantedFiles)
    {
        await _client.TorrentSetAsync(new TorrentSettings
        {
            Ids = [downloadId],
            FilesUnwanted = unwantedFiles,
        });
    }
    
    private async Task<(bool, DeleteReason)> EvaluateDownloadRemoval(TorrentInfo download, bool isPrivate)
    {
        (bool ShouldRemove, DeleteReason Reason) result = await BlockUnwantedFilesAsync(download, isPrivate);

        if (result.ShouldRemove)
        {
            return result;
        }
        
        result = await CheckIfSlow(download);

        if (result.ShouldRemove)
        {
            return result;
        }

        return await CheckIfStuck(download);
    }

    private async Task<(bool ShouldRemove, DeleteReason Reason)> CheckIfSlow(TorrentInfo download)
    {
        var queueCleanerConfig = ContextProvider.Get<QueueCleanerConfig>(nameof(QueueCleanerConfig));
        
        if (queueCleanerConfig.Slow.MaxStrikes is 0)
        {
            return (false, DeleteReason.None);
        }
        
        if (download.Status is not 4)
        {
            // not in downloading state
            return (false, DeleteReason.None);
        }
        
        if (download.RateDownload <= 0)
        {
            return (false, DeleteReason.None);
        }
        
        if (queueCleanerConfig.Slow.IgnorePrivate && download.IsPrivate is true)
        {
            // ignore private trackers
            _logger.LogDebug("skip slow check | download is private | {name}", download.Name);
            return (false, DeleteReason.None);
        }

        if (download.TotalSize > (queueCleanerConfig.Slow.IgnoreAboveSizeByteSize?.Bytes ?? long.MaxValue))
        {
            _logger.LogDebug("skip slow check | download is too large | {name}", download.Name);
            return (false, DeleteReason.None);
        }
        
        ByteSize minSpeed = queueCleanerConfig.Slow.MinSpeedByteSize;
        ByteSize currentSpeed = new ByteSize(download.RateDownload ?? long.MaxValue);
        SmartTimeSpan maxTime = SmartTimeSpan.FromHours(queueCleanerConfig.Slow.MaxTime);
        SmartTimeSpan currentTime = SmartTimeSpan.FromSeconds(download.Eta ?? 0);

        return await CheckIfSlow(
            download.HashString!,
            download.Name!,
            minSpeed,
            currentSpeed,
            maxTime,
            currentTime
        );
    }

    private async Task<(bool ShouldRemove, DeleteReason Reason)> CheckIfStuck(TorrentInfo download)
    {
        var queueCleanerConfig = ContextProvider.Get<QueueCleanerConfig>(nameof(QueueCleanerConfig));
        
        if (queueCleanerConfig.Stalled.MaxStrikes is 0)
        {
            return (false, DeleteReason.None);
        }
        
        if (download.Status is not 4)
        {
            // not in downloading state
            return (false, DeleteReason.None);
        }
        
        if (download.RateDownload > 0 || download.Eta > 0)
        {
            return (false, DeleteReason.None);
        }
        
        if (queueCleanerConfig.Stalled.IgnorePrivate && (download.IsPrivate ?? false))
        {
            // ignore private trackers
            _logger.LogDebug("skip stalled check | download is private | {name}", download.Name);
            return (false, DeleteReason.None);
        }
        
        ResetStalledStrikesOnProgress(download.HashString!, download.DownloadedEver ?? 0);
        
        return (await _striker.StrikeAndCheckLimit(download.HashString!, download.Name!, queueCleanerConfig.Stalled.MaxStrikes, StrikeType.Stalled), DeleteReason.Stalled);
    }
}