using Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Events;

/// <summary>
/// SignalR hub for real-time event delivery
/// </summary>
public class EventHub : Hub
{
    private readonly DataContext _context;
    private readonly ILogger<EventHub> _logger;

    public EventHub(DataContext context, ILogger<EventHub> logger)
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
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Client disconnected
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
} 