using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cleanuparr.Domain.Exceptions;
using Cleanuparr.Persistence.Models.Configuration;
using Cleanuparr.Shared.Attributes;

namespace Cleanuparr.Persistence.Models.Configuration.Notification;

public sealed record NotifiarrConfig : IConfig
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [Required]
    public Guid NotificationConfigId { get; init; }
    
    [ForeignKey(nameof(NotificationConfigId))]
    public NotificationConfig NotificationConfig { get; init; } = null!;
    
    [Required]
    [MaxLength(255)]
    [SensitiveData]
    public string ApiKey { get; init; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string ChannelId { get; init; } = string.Empty;
    
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ApiKey) && 
               !string.IsNullOrWhiteSpace(ChannelId);
    }
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new CustomValidationException("Notifiarr API key is required");
        }
        
        if (ApiKey.Length < 10)
        {
            throw new CustomValidationException("Notifiarr API key must be at least 10 characters long");
        }
        
        if (string.IsNullOrWhiteSpace(ChannelId))
        {
            throw new CustomValidationException("Discord channel ID is required");
        }
        
        if (!ulong.TryParse(ChannelId, out _))
        {
            throw new CustomValidationException("Discord channel ID must be a valid numeric ID");
        }
    }
}
