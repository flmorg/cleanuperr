using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Data;
using Data.Models.Events;

namespace Infrastructure.Events;

/// <summary>
/// Service for publishing events to database and SignalR hub
/// </summary>
public class EventPublisher
{
    private readonly DataContext _context;
    private readonly IHubContext<EventHub> _hubContext;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(
        DataContext context, 
        IHubContext<EventHub> hubContext, 
        ILogger<EventPublisher> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Publishes an event to database and SignalR clients
    /// </summary>
    public async Task PublishAsync(string eventType, string source, string message, string severity = "Info", object? data = null, string? correlationId = null)
    {
        var eventEntity = new AppEvent
        {
            EventType = eventType,
            Source = source,
            Message = message,
            Severity = severity,
            Data = data != null ? JsonSerializer.Serialize(data) : null,
            CorrelationId = correlationId
        };

        // Save to database
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Send to SignalR clients
        await NotifyClientsAsync(eventEntity);

        _logger.LogTrace("Published event: {eventType} from {source}", eventType, source);
    }

    /// <summary>
    /// Publishes an info event
    /// </summary>
    public async Task PublishInfoAsync(string source, string message, object? data = null, string? correlationId = null)
    {
        await PublishAsync("Information", source, message, "Info", data, correlationId);
    }

    /// <summary>
    /// Publishes a warning event
    /// </summary>
    public async Task PublishWarningAsync(string source, string message, object? data = null, string? correlationId = null)
    {
        await PublishAsync("Warning", source, message, "Warning", data, correlationId);
    }

    /// <summary>
    /// Publishes an error event
    /// </summary>
    public async Task PublishErrorAsync(string source, string message, object? data = null, string? correlationId = null)
    {
        await PublishAsync("Error", source, message, "Error", data, correlationId);
    }

    /// <summary>
    /// Publishes a notification-related event (for HTTP notifications to Notifiarr/Apprise)
    /// </summary>
    public async Task PublishNotificationEventAsync(string provider, string message, bool success, object? data = null, string? correlationId = null)
    {
        var eventType = success ? "NotificationSent" : "NotificationFailed";
        var severity = success ? "Info" : "Warning";
        
        await PublishAsync(eventType, $"NotificationService.{provider}", message, severity, data, correlationId);
    }

    /// <summary>
    /// Publishes an HTTP call event (for external API calls)
    /// </summary>
    public async Task PublishHttpCallEventAsync(string endpoint, string method, int statusCode, TimeSpan duration, object? data = null, string? correlationId = null)
    {
        var success = statusCode >= 200 && statusCode < 300;
        var eventType = success ? "HttpCallSuccess" : "HttpCallFailed";
        var severity = success ? "Info" : "Warning";
        var message = $"{method} {endpoint} - {statusCode} ({duration.TotalMilliseconds}ms)";
        
        await PublishAsync(eventType, "HttpClient", message, severity, data, correlationId);
    }

    private async Task NotifyClientsAsync(AppEvent appEventEntity)
    {
        try
        {
            // Send to all connected clients (self-hosted app with single client)
            await _hubContext.Clients.All.SendAsync("EventReceived", appEventEntity);

            _logger.LogTrace("Sent event {eventId} to SignalR clients", appEventEntity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send event {eventId} to SignalR clients", appEventEntity.Id);
        }
    }
} 