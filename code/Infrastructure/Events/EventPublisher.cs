using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Data;
using Data.Enums;
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
    public async Task PublishAsync(EventType eventType, string message, EventSeverity severity, object? data = null, Guid? trackingId = null)
    {
        var eventEntity = new AppEvent
        {
            EventType = eventType,
            Message = message,
            Severity = severity,
            Data = data != null ? JsonSerializer.Serialize(data) : null,
            TrackingId = trackingId
        };

        // Save to database
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Send to SignalR clients
        await NotifyClientsAsync(eventEntity);

        _logger.LogTrace("Published event: {eventType}", eventType);
    }

    private async Task NotifyClientsAsync(AppEvent appEventEntity)
    {
        try
        {
            // Send to all connected clients (self-hosted app with single client)
            await _hubContext.Clients.All.SendAsync("EventReceived", appEventEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send event {eventId} to SignalR clients", appEventEntity.Id);
        }
    }
} 