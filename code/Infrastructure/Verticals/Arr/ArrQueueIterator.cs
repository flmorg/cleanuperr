﻿using Common.Configuration;
using Domain.Arr.Queue;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.Arr;

public sealed class ArrQueueIterator
{
    private readonly ILogger<ArrQueueIterator> _logger;
    
    public ArrQueueIterator(ILogger<ArrQueueIterator> logger)
    {
        _logger = logger;
    }
    
    public async Task Iterate(ArrClient arrClient, ArrInstance arrInstance, Func<IReadOnlyList<QueueRecord>, Task> action)
    {
        const ushort maxPage = 100;
        ushort page = 1;
        int totalRecords = 0;
        int processedRecords = 0;

        do
        {
            QueueListResponse queueResponse = await arrClient.GetQueueItemsAsync(arrInstance, page);
            
            if (totalRecords is 0)
            {
                totalRecords = queueResponse.TotalRecords;
                
                _logger.LogInformation(
                    "{items} items found in queue | {url}",
                    queueResponse.TotalRecords, arrInstance.Url);
            }

            if (queueResponse.Records.Count is 0)
            {
                break;
            }
            
            await action(queueResponse.Records);

            processedRecords += queueResponse.Records.Count;

            if (processedRecords >= totalRecords)
            {
                break;
            }

            page++;
        } while (processedRecords < totalRecords && page < maxPage);
    }
}