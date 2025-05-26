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
} 