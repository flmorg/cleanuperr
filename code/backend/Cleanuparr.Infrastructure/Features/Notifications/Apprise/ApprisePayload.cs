using System.ComponentModel.DataAnnotations;

namespace Cleanuparr.Infrastructure.Features.Notifications.Apprise;

public sealed record ApprisePayload
{
    [Required]
    public string Title { get; init; }
    
    [Required]
    public string Body { get; init; }

    public string Type { get; init; } = NotificationType.Info.ToString().ToLowerInvariant();

    public string Format { get; init; } = FormatType.Text.ToString().ToLowerInvariant();
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Failure
}

public enum FormatType
{
    Text,
    Markdown,
    Html
}