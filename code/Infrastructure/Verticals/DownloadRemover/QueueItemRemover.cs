using Common.Configuration.Arr;
using Common.Configuration.General;
using Domain.Enums;
using Domain.Models.Arr;
using Domain.Models.Arr.Queue;
using Infrastructure.Helpers;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.DownloadRemover.Interfaces;
using Infrastructure.Verticals.DownloadRemover.Models;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.DownloadRemover;

public sealed class QueueItemRemover : IQueueItemRemover
{
    private readonly SearchConfig _searchConfig;
    private readonly IMemoryCache _cache;
    private readonly ArrClientFactory _arrClientFactory;
    private readonly INotificationPublisher _notifier;

    public QueueItemRemover(
        IOptions<SearchConfig> searchConfig,
        IMemoryCache cache,
        ArrClientFactory arrClientFactory,
        INotificationPublisher notifier
    )
    {
        _searchConfig = searchConfig.Value;
        _cache = cache;
        _arrClientFactory = arrClientFactory;
        _notifier = notifier;
    }

    public async Task RemoveQueueItemAsync<T>(QueueItemRemoveRequest<T> request)
        where T : SearchItem
    {
        try
        {
            var arrClient = _arrClientFactory.GetClient(request.InstanceType);
            await arrClient.DeleteQueueItemAsync(request.Instance, request.Record, request.RemoveFromClient, request.DeleteReason);
            
            // push to context
            ContextProvider.Set(nameof(QueueRecord), request.Record);
            ContextProvider.Set(nameof(ArrInstance) + nameof(ArrInstance.Url), request.Instance.Url);
            ContextProvider.Set(nameof(InstanceType), request.InstanceType);
            await _notifier.NotifyQueueItemDeleted(request.RemoveFromClient, request.DeleteReason);

            if (!_searchConfig.SearchEnabled)
            {
                return;
            }

            await arrClient.SearchItemsAsync(request.Instance, [request.SearchItem]);

            // prevent tracker spamming
            await Task.Delay(TimeSpan.FromSeconds(_searchConfig.SearchDelay));
        }
        finally
        {
            _cache.Remove(CacheKeys.DownloadMarkedForRemoval(request.Record.DownloadId, request.Instance.Url));
        }
    }
}