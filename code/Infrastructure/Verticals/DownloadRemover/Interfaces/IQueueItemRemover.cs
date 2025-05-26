using Data.Models.Arr;
using Infrastructure.Verticals.DownloadRemover.Models;

namespace Infrastructure.Verticals.DownloadRemover.Interfaces;

public interface IQueueItemRemover
{
    Task RemoveQueueItemAsync<T>(QueueItemRemoveRequest<T> request) where T : SearchItem;
}