using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Events;

/// <summary>
/// Extension methods for easy event publishing
/// </summary>
public static class EventExtensions
{
    /// <summary>
    /// Publishes an information event
    /// </summary>
    public static async Task PublishInfoEventAsync(this EventDbContext context, string source, string message, object? data = null, string? correlationId = null)
    {
        await PublishEventAsync(context, "Information", source, message, "Info", data, correlationId);
    }

    /// <summary>
    /// Publishes a warning event
    /// </summary>
    public static async Task PublishWarningEventAsync(this EventDbContext context, string source, string message, object? data = null, string? correlationId = null)
    {
        await PublishEventAsync(context, "Warning", source, message, "Warning", data, correlationId);
    }

    /// <summary>
    /// Publishes an error event
    /// </summary>
    public static async Task PublishErrorEventAsync(this EventDbContext context, string source, string message, object? data = null, string? correlationId = null)
    {
        await PublishEventAsync(context, "Error", source, message, "Error", data, correlationId);
    }

    /// <summary>
    /// Publishes a custom event
    /// </summary>
    public static async Task PublishEventAsync(this EventDbContext context, string eventType, string source, string message, string severity = "Info", object? data = null, string? correlationId = null)
    {
        var eventEntity = new Event
        {
            EventType = eventType,
            Source = source,
            Message = message,
            Severity = severity,
            Data = data != null ? JsonSerializer.Serialize(data) : null,
            CorrelationId = correlationId
        };

        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Publishes an information event synchronously
    /// </summary>
    public static void PublishInfoEvent(this EventDbContext context, string source, string message, object? data = null, string? correlationId = null)
    {
        PublishEvent(context, "Information", source, message, "Info", data, correlationId);
    }

    /// <summary>
    /// Publishes a warning event synchronously
    /// </summary>
    public static void PublishWarningEvent(this EventDbContext context, string source, string message, object? data = null, string? correlationId = null)
    {
        PublishEvent(context, "Warning", source, message, "Warning", data, correlationId);
    }

    /// <summary>
    /// Publishes an error event synchronously
    /// </summary>
    public static void PublishErrorEvent(this EventDbContext context, string source, string message, object? data = null, string? correlationId = null)
    {
        PublishEvent(context, "Error", source, message, "Error", data, correlationId);
    }

    /// <summary>
    /// Publishes a custom event synchronously
    /// </summary>
    public static void PublishEvent(this EventDbContext context, string eventType, string source, string message, string severity = "Info", object? data = null, string? correlationId = null)
    {
        var eventEntity = new Event
        {
            EventType = eventType,
            Source = source,
            Message = message,
            Severity = severity,
            Data = data != null ? JsonSerializer.Serialize(data) : null,
            CorrelationId = correlationId
        };

        context.Events.Add(eventEntity);
        context.SaveChanges();
    }

    /// <summary>
    /// Gets recent events
    /// </summary>
    public static async Task<List<Event>> GetRecentEventsAsync(this EventDbContext context, int count = 100)
    {
        return await context.Events
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Gets events by type
    /// </summary>
    public static async Task<List<Event>> GetEventsByTypeAsync(this EventDbContext context, string eventType, int count = 100)
    {
        return await context.Events
            .Where(e => e.EventType == eventType)
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Gets events by severity
    /// </summary>
    public static async Task<List<Event>> GetEventsBySeverityAsync(this EventDbContext context, string severity, int count = 100)
    {
        return await context.Events
            .Where(e => e.Severity == severity)
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Cleans up events older than the specified number of days
    /// </summary>
    public static async Task<int> CleanupOldEventsAsync(this EventDbContext context, int retentionDays = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        var oldEvents = context.Events.Where(e => e.Timestamp < cutoffDate);
        var count = await oldEvents.CountAsync();
        
        if (count > 0)
        {
            context.Events.RemoveRange(oldEvents);
            await context.SaveChangesAsync();
        }
        
        return count;
    }
}

/// <summary>
/// Service collection extensions for event system
/// </summary>
public static class EventServiceExtensions
{
    /// <summary>
    /// Adds event system with SQLite database
    /// </summary>
    public static IServiceCollection AddEventSystem(this IServiceCollection services)
    {
        services.AddDbContext<EventDbContext>();
        services.AddScoped<EventPublisher>();
        services.AddSingleton<EventCleanupService>();
        services.AddHostedService<EventCleanupService>();
        services.AddSignalR();
        return services;
    }
} 