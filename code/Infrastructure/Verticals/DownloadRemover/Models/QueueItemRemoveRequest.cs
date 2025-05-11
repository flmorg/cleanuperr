using Common.Configuration.Arr;
using Domain.Enums;
using Domain.Models.Arr;
using Domain.Models.Arr.Queue;

namespace Infrastructure.Verticals.DownloadRemover.Models;

public sealed record QueueItemRemoveRequest<T>
    where T : SearchItem
{
    public required InstanceType InstanceType { get; init; }
    
    public required ArrInstance Instance { get; init; }
    
    public required T SearchItem { get; init; }
    
    public required QueueRecord Record { get; init; }
    
    public required bool RemoveFromClient { get; init; }
    
    public required DeleteReason DeleteReason { get; init; }
}