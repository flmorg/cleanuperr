using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Attributes;
using Data.Models.Configuration.QueueCleaner;
using Common.CustomDataTypes;
using Data.Enums;
using Data.Models.Deluge.Response;
using Infrastructure.Extensions;
using Infrastructure.Verticals.Context;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.DownloadClient.Deluge;

public partial class DelugeService
{
    /// <inheritdoc/>
    public override async Task<DownloadCheckResult> ShouldRemoveFromArrQueueAsync(string hash,
        IReadOnlyList<string> ignoredDownloads)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("Deluge client is not initialized");
        }
        
        hash = hash.ToLowerInvariant();
        
        DelugeContents? contents = null;
        DownloadCheckResult result = new();

        DownloadStatus? download = await _client.GetTorrentStatus(hash);
        
        if (download?.Hash is null)
        {
            _logger.LogDebug("failed to find torrent {hash} in the download client", hash);
            return result;
        }
        
        result.Found = true;
        result.IsPrivate = download.Private;
        
        if (ignoredDownloads.Count > 0 && download.ShouldIgnore(ignoredDownloads))
        {
            _logger.LogInformation("skip | download is ignored | {name}", download.Name);
            return result;
        }

        try
        {
            contents = await _client.GetTorrentFiles(hash);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "failed to find torrent {hash} in the download client", hash);
        }
        

        bool shouldRemove = contents?.Contents?.Count > 0;
        
        ProcessFiles(contents.Contents, (_, file) =>
        {
            if (file.Priority > 0)
            {
                shouldRemove = false;
            }
        });

        if (shouldRemove)
        {
            // remove if all files are unwanted
            result.ShouldRemove = true;
            result.DeleteReason = DeleteReason.AllFilesSkipped;
            return result;
        }
        
        // remove if download is stuck
        (result.ShouldRemove, result.DeleteReason) = await EvaluateDownloadRemoval(download, download.Private, contents.Contents);

        return result;
    }

    private async Task<(bool ShouldRemove, DeleteReason Reason)> BlockUnwantedFilesAsync(
        DownloadStatus download,
        bool isPrivate,
        Dictionary<string, DelugeFileOrDirectory>? files
    )
    {
        if (!_queueCleanerConfig.ContentBlocker.Enabled)
        {
            return (false, DeleteReason.None);
        }

        if (_queueCleanerConfig.ContentBlocker.IgnorePrivate && isPrivate)
        {
            // ignore private trackers
            _logger.LogDebug("skip unwanted files check | download is private | {name}", download.Name);
            return (false, DeleteReason.None);
        }
        
        if (files is null)
        {
            _logger.LogDebug("failed to find files for {name}", download.Name);
            return (false, DeleteReason.None);
        }
        
        InstanceType instanceType = (InstanceType)ContextProvider.Get<object>(nameof(InstanceType));
        BlocklistType blocklistType = _blocklistProvider.GetBlocklistType(instanceType);
        ConcurrentBag<string> patterns = _blocklistProvider.GetPatterns(instanceType);
        ConcurrentBag<Regex> regexes = _blocklistProvider.GetRegexes(instanceType);
        
        Dictionary<int, int> priorities = [];
        bool hasPriorityUpdates = false;
        long totalFiles = 0;
        long totalUnwantedFiles = 0;

        ProcessFiles(files, (name, file) =>
        {
            totalFiles++;
            int priority = file.Priority;

            if (file.Priority is 0)
            {
                totalUnwantedFiles++;
            }

            if (file.Priority is not 0 && !_filenameEvaluator.IsValid(name, blocklistType, patterns, regexes))
            {
                totalUnwantedFiles++;
                priority = 0;
                hasPriorityUpdates = true;
                _logger.LogInformation("unwanted file found | {file}", file.Path);
            }
            
            priorities.Add(file.Index, priority);
        });

        if (!hasPriorityUpdates)
        {
            return (false, DeleteReason.None);
        }
        
        _logger.LogTrace("marking {count} unwanted files as skipped for {name}", totalUnwantedFiles, download.Name);

        List<int> sortedPriorities = priorities
            .OrderBy(x => x.Key)
            .Select(x => x.Value)
            .ToList();

        if (totalUnwantedFiles == totalFiles)
        {
            // Skip marking files as unwanted. The download will be removed completely.
            return (true, DeleteReason.AllFilesBlocked);
        }

        await _dryRunInterceptor.InterceptAsync(ChangeFilesPriority, download.Hash, sortedPriorities);

        return (false, DeleteReason.None);
    }

    [DryRunSafeguard]
    protected virtual async Task ChangeFilesPriority(string hash, List<int> sortedPriorities)
    {
        await _client.ChangeFilesPriority(hash, sortedPriorities);
    }
    
    private async Task<(bool, DeleteReason)> EvaluateDownloadRemoval(
        DownloadStatus status,
        bool isPrivate,
        Dictionary<string, DelugeFileOrDirectory>? files
    )
    {
        (bool ShouldRemove, DeleteReason Reason) result = await BlockUnwantedFilesAsync(status, isPrivate, files);

        if (result.ShouldRemove)
        {
            return result;
        }
        
        result = await CheckIfSlow(status);

        if (result.ShouldRemove)
        {
            return result;
        }

        return await CheckIfStuck(status);
    }
    
    private async Task<(bool ShouldRemove, DeleteReason Reason)> CheckIfSlow(DownloadStatus download)
    {
        if (_queueCleanerConfig.Slow.MaxStrikes is 0)
        {
            return (false, DeleteReason.None);
        }
        
        if (download.State is null || !download.State.Equals("Downloading", StringComparison.InvariantCultureIgnoreCase))
        {
            return (false, DeleteReason.None);
        }
        
        if (download.DownloadSpeed <= 0)
        {
            return (false, DeleteReason.None);
        }
        
        if (_queueCleanerConfig.Slow.IgnorePrivate && download.Private)
        {
            // ignore private trackers
            _logger.LogDebug("skip slow check | download is private | {name}", download.Name);
            return (false, DeleteReason.None);
        }
        
        if (download.Size > (_queueCleanerConfig.Slow.IgnoreAboveSizeByteSize?.Bytes ?? long.MaxValue))
        {
            _logger.LogDebug("skip slow check | download is too large | {name}", download.Name);
            return (false, DeleteReason.None);
        }
        
        ByteSize minSpeed = _queueCleanerConfig.Slow.MinSpeedByteSize;
        ByteSize currentSpeed = new ByteSize(download.DownloadSpeed);
        SmartTimeSpan maxTime = SmartTimeSpan.FromHours(_queueCleanerConfig.Slow.MaxTime);
        SmartTimeSpan currentTime = SmartTimeSpan.FromSeconds(download.Eta);

        return await CheckIfSlow(
            download.Hash!,
            download.Name!,
            minSpeed,
            currentSpeed,
            maxTime,
            currentTime
        );
    }
    
    private async Task<(bool ShouldRemove, DeleteReason Reason)> CheckIfStuck(DownloadStatus status)
    {
        if (_queueCleanerConfig.Stalled.MaxStrikes is 0)
        {
            return (false, DeleteReason.None);
        }
        
        if (_queueCleanerConfig.Stalled.IgnorePrivate && status.Private)
        {
            // ignore private trackers
            _logger.LogDebug("skip stalled check | download is private | {name}", status.Name);
            return (false, DeleteReason.None);
        }
        
        if (status.State is null || !status.State.Equals("Downloading", StringComparison.InvariantCultureIgnoreCase))
        {
            return (false, DeleteReason.None);
        }

        if (status.Eta > 0)
        {
            return (false, DeleteReason.None);
        }
        
        ResetStalledStrikesOnProgress(status.Hash!, status.TotalDone);
        
        return (await _striker.StrikeAndCheckLimit(status.Hash!, status.Name!, _queueCleanerConfig.Stalled.MaxStrikes, StrikeType.Stalled), DeleteReason.Stalled);
    }
}