using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cleanuparr.Domain.Exceptions;
using Cleanuparr.Shared.Attributes;

namespace Cleanuparr.Persistence.Models.Configuration.Notification;

public sealed record GotifyConfig : IConfig
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required]
    public Guid NotificationConfigId { get; init; }

    [ForeignKey(nameof(NotificationConfigId))]
    public NotificationConfig NotificationConfig { get; init; } = null!;

    [Required]
    [MaxLength(500)]
    public string ServerUrl { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [SensitiveData]
    public string ApplicationToken { get; init; } = string.Empty;

    public int Priority { get; init; } = 5;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ServerUrl) && !string.IsNullOrWhiteSpace(ApplicationToken);
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ServerUrl))
        {
            throw new CustomValidationException("Gotify server URL is required");
        }

        if (!Uri.TryCreate(ServerUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            throw new CustomValidationException("Gotify server URL must be a valid HTTP or HTTPS URL");
        }

        if (string.IsNullOrWhiteSpace(ApplicationToken))
        {
            throw new CustomValidationException("Gotify application token is required");
        }

        if (Priority < 0 || Priority > 10)
        {
            throw new CustomValidationException("Priority must be between 0 and 10");
        }
    }
}
