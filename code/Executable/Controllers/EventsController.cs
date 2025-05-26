using Data;
using Data.Models.Events;
using Infrastructure.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Executable.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly DataContext _context;

    public EventsController(DataContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets recent events
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AppEvent>>> GetEvents(
        [FromQuery] int count = 100,
        [FromQuery] string? severity = null,
        [FromQuery] string? eventType = null,
        [FromQuery] string? source = null)
    {
        var query = _context.Events.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(e => e.Severity == severity);

        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(e => e.EventType == eventType);

        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(e => e.Source.Contains(source));

        // Order and limit
        var events = await query
            .OrderByDescending(e => e.Timestamp)
            .Take(Math.Min(count, 1000)) // Cap at 1000
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>
    /// Gets a specific event by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AppEvent>> GetEvent(string id)
    {
        var eventEntity = await _context.Events.FindAsync(id);
        
        if (eventEntity == null)
            return NotFound();

        return Ok(eventEntity);
    }

    /// <summary>
    /// Gets events by correlation ID
    /// </summary>
    [HttpGet("correlation/{correlationId}")]
    public async Task<ActionResult<List<AppEvent>>> GetEventsByCorrelation(string correlationId)
    {
        var events = await _context.Events
            .Where(e => e.CorrelationId == correlationId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>
    /// Gets event statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetEventStats()
    {
        var stats = new
        {
            TotalEvents = await _context.Events.CountAsync(),
            EventsBySeverity = await _context.Events
                .GroupBy(e => e.Severity)
                .Select(g => new { Severity = g.Key, Count = g.Count() })
                .ToListAsync(),
            EventsByType = await _context.Events
                .GroupBy(e => e.EventType)
                .Select(g => new { EventType = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync(),
            RecentEventsCount = await _context.Events
                .Where(e => e.Timestamp > DateTime.UtcNow.AddHours(-24))
                .CountAsync()
        };

        return Ok(stats);
    }

    /// <summary>
    /// Manually triggers cleanup of old events
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<ActionResult<object>> CleanupOldEvents([FromQuery] int retentionDays = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        
        await _context.Events
            .Where(e => e.Timestamp < cutoffDate)
            .ExecuteDeleteAsync();
        
        return Ok();
    }

    /// <summary>
    /// Gets unique event sources
    /// </summary>
    [HttpGet("sources")]
    public async Task<ActionResult<List<string>>> GetEventSources()
    {
        var sources = await _context.Events
            .Select(e => e.Source)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        return Ok(sources);
    }

    /// <summary>
    /// Gets unique event types
    /// </summary>
    [HttpGet("types")]
    public async Task<ActionResult<List<string>>> GetEventTypes()
    {
        var types = await _context.Events
            .Select(e => e.EventType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        return Ok(types);
    }
} 