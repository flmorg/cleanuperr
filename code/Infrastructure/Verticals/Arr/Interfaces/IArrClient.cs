using Common.Configuration.Arr;
using Domain.Enums;
using Domain.Models.Arr;
using Domain.Models.Arr.Queue;

namespace Infrastructure.Verticals.Arr.Interfaces;

public interface IArrClient
{
    Task<QueueListResponse> GetQueueItemsAsync(ArrInstance arrInstance, int page);

    Task<bool> ShouldRemoveFromQueue(InstanceType instanceType, QueueRecord record, bool isPrivateDownload);

    Task DeleteQueueItemAsync(ArrInstance arrInstance, QueueRecord record, bool removeFromClient, DeleteReason deleteReason);
    
    Task RefreshItemsAsync(ArrInstance arrInstance, HashSet<SearchItem>? items);
    
    bool IsRecordValid(QueueRecord record);
}