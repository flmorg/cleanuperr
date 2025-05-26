using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Events;

/// <summary>
/// SignalR hub for real-time event delivery
/// </summary>
public class EventHub : Hub
{
    private readonly EventDbContext _context;
    private readonly ILogger<EventHub> _logger;

    public EventHub(EventDbContext context, ILogger<EventHub> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Client connects and subscribes to all events
    /// </summary>
    public async Task JoinEventsGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Events");
        _logger.LogTrace("Client {connectionId} joined Events group", Context.ConnectionId);
    }

    /// <summary>
    /// Client unsubscribes from all events
    /// </summary>
    public async Task LeaveEventsGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Events");
        _logger.LogTrace("Client {connectionId} left Events group", Context.ConnectionId);
    }

    /// <summary>
    /// Client subscribes to specific severity level
    /// </summary>
    public async Task JoinSeverityGroup(string severity)
    {
        if (IsValidSeverity(severity))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Events_{severity}");
            _logger.LogTrace("Client {connectionId} joined severity group {severity}", Context.ConnectionId, severity);
        }
    }

    /// <summary>
    /// Client unsubscribes from specific severity level
    /// </summary>
    public async Task LeaveSeverityGroup(string severity)
    {
        if (IsValidSeverity(severity))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Events_{severity}");
            _logger.LogTrace("Client {connectionId} left severity group {severity}", Context.ConnectionId, severity);
        }
    }

    /// <summary>
    /// Client subscribes to specific event type
    /// </summary>
    public async Task JoinTypeGroup(string eventType)
    {
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Events_{eventType}");
            _logger.LogTrace("Client {connectionId} joined type group {eventType}", Context.ConnectionId, eventType);
        }
    }

    /// <summary>
    /// Client unsubscribes from specific event type
    /// </summary>
    public async Task LeaveTypeGroup(string eventType)
    {
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Events_{eventType}");
            _logger.LogTrace("Client {connectionId} left type group {eventType}", Context.ConnectionId, eventType);
        }
    }

    /// <summary>
    /// Client requests recent events (for initial load)
    /// </summary>
    public async Task GetRecentEvents(int count = 50)
    {
        try
        {
            var events = await _context.Events
                .OrderByDescending(e => e.Timestamp)
                .Take(Math.Min(count, 100)) // Cap at 100
                .ToListAsync();

            await Clients.Caller.SendAsync("RecentEventsReceived", events);
            _logger.LogTrace("Sent {count} recent events to client {connectionId}", events.Count, Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send recent events to client {connectionId}", Context.ConnectionId);
        }
    }

    /// <summary>
    /// Client connection established
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogTrace("Client {connectionId} connected to EventHub", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Client disconnected
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogTrace("Client {connectionId} disconnected from EventHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private static bool IsValidSeverity(string severity)
    {
        return severity is "Info" or "Warning" or "Error" or "Critical";
    }
} 