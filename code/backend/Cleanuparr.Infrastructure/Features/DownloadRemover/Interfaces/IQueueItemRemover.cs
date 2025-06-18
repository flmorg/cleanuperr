using Cleanuparr.Infrastructure.Features.DownloadRemover.Models;
using Data.Models.Arr;

namespace Cleanuparr.Infrastructure.Features.DownloadRemover.Interfaces;

public interface IQueueItemRemover
{
    Task RemoveQueueItemAsync<T>(QueueItemRemoveRequest<T> request) where T : SearchItem;
}