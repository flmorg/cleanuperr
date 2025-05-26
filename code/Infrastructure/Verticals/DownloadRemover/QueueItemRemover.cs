using Common.Configuration.Arr;
using Common.Configuration.General;
using Data.Enums;
using Data.Models.Arr;
using Data.Models.Arr.Queue;
using Infrastructure.Helpers;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.DownloadRemover.Interfaces;
using Infrastructure.Verticals.DownloadRemover.Models;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Caching.Memory;
using Infrastructure.Configuration;

namespace Infrastructure.Verticals.DownloadRemover;

public sealed class QueueItemRemover : IQueueItemRemover
{
    private readonly GeneralConfig _generalConfig;
    private readonly IMemoryCache _cache;
    private readonly ArrClientFactory _arrClientFactory;
    private readonly INotificationPublisher _notifier;

    public QueueItemRemover(
        IConfigManager configManager,
        IMemoryCache cache,
        ArrClientFactory arrClientFactory,
        INotificationPublisher notifier
    )
    {
        _generalConfig = configManager.GetConfiguration<GeneralConfig>();
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

            if (!_generalConfig.SearchEnabled)
            {
                return;
            }

            await arrClient.SearchItemsAsync(request.Instance, [request.SearchItem]);

            // prevent tracker spamming
            await Task.Delay(TimeSpan.FromSeconds(_generalConfig.SearchDelay));
        }
        finally
        {
            _cache.Remove(CacheKeys.DownloadMarkedForRemoval(request.Record.DownloadId, request.Instance.Url));
        }
    }
}