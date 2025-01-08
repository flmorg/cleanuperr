namespace Domain.Models.Arr.Blocking;

public sealed record RadarrBlockedItem : BlockedItem
{
    public required long MovieId { get; init; }
}