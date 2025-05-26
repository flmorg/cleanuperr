using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Events;

/// <summary>
/// Represents an event in the system
/// </summary>
[Index(nameof(Timestamp), IsDescending = new[] { true })]
[Index(nameof(EventType))]
[Index(nameof(Severity))]
[Index(nameof(Source))]
public class Event
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Source { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON data associated with the event
    /// </summary>
    public string? Data { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = "Info"; // Info, Warning, Error, Critical
    
    /// <summary>
    /// Optional correlation ID to link related events
    /// </summary>
    [MaxLength(50)]
    public string? CorrelationId { get; set; }
} 