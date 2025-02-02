using Domain.Enums;

namespace Infrastructure.Verticals.Notifications.Models;

public record Notification
{
    public required InstanceType InstanceType { get; init; }
    
    public required Uri InstanceUrl { get; init; }
    
    public required string Hash { get; init; }
    
    public required string Title { get; init; }
    
    public required string Description { get; init; }
    
    public Uri? Image { get; init; }
    
    public List<NotificationField>? Fields { get; init; }
}