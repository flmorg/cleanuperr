﻿using Common.Configuration;
using Common.Configuration.Arr;
using Domain.Enums;
using Domain.Models.Arr.Queue;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.ContentBlocker;

public sealed class ContentBlocker : GenericHandler
{
    private readonly BlocklistProvider _blocklistProvider;
    
    public ContentBlocker(
        ILogger<ContentBlocker> logger,
        IOptions<SonarrConfig> sonarrConfig,
        IOptions<RadarrConfig> radarrConfig,
        SonarrClient sonarrClient,
        RadarrClient radarrClient,
        ArrQueueIterator arrArrQueueIterator,
        BlocklistProvider blocklistProvider,
        DownloadServiceFactory downloadServiceFactory
    ) : base(logger, sonarrConfig.Value, radarrConfig.Value, sonarrClient, radarrClient, arrArrQueueIterator, downloadServiceFactory)
    {
        _blocklistProvider = blocklistProvider;
    }

    public override async Task ExecuteAsync()
    {
        await _blocklistProvider.LoadBlocklistAsync();
        await base.ExecuteAsync();
    }

    protected override async Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType)
    {
        ArrClient arrClient = GetClient(instanceType);

        await _arrArrQueueIterator.Iterate(arrClient, instance, async items =>
        {
            foreach (QueueRecord record in items)
            {
                if (record.Protocol is not "torrent")
                {
                    continue;
                }
                
                if (string.IsNullOrEmpty(record.DownloadId))
                {
                    _logger.LogDebug("skip | download id is null for {title}", record.Title);
                    continue;
                }
                
                _logger.LogDebug("searching unwanted files for {title}", record.Title);
                await _downloadService.BlockUnwantedFilesAsync(record.DownloadId);
            }
        });
    }
}