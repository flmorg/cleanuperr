using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Events;

/// <summary>
/// Background service that periodically cleans up old events
/// </summary>
public class EventCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(4); // Run every 4 hours
    private readonly int _retentionDays = 30; // Keep events for 30 days

    public EventCleanupService(IServiceProvider serviceProvider, ILogger<EventCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event cleanup service started. Interval: {interval}, Retention: {retention} days", 
            _cleanupInterval, _retentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
                
                if (stoppingToken.IsCancellationRequested)
                    break;

                await PerformCleanupAsync();
            }
            catch (OperationCanceledException)
            {
                // Service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during event cleanup");
            }
        }

        _logger.LogInformation("Event cleanup service stopped");
    }

    private async Task PerformCleanupAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EventDbContext>();

            var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);
            var oldEventsQuery = context.Events.Where(e => e.Timestamp < cutoffDate);
            var count = await oldEventsQuery.CountAsync();

            if (count > 0)
            {
                _logger.LogInformation("Cleaning up {count} events older than {cutoffDate:yyyy-MM-dd}", count, cutoffDate);
                
                // Remove in batches to avoid large transactions
                const int batchSize = 1000;
                var totalRemoved = 0;

                while (true)
                {
                    var batch = await oldEventsQuery.Take(batchSize).ToListAsync();
                    if (batch.Count == 0)
                        break;

                    context.Events.RemoveRange(batch);
                    await context.SaveChangesAsync();
                    totalRemoved += batch.Count;

                    _logger.LogTrace("Removed batch of {batchCount} events, total removed: {totalRemoved}", batch.Count, totalRemoved);
                }

                _logger.LogInformation("Event cleanup completed. Removed {totalRemoved} old events", totalRemoved);
            }
            else
            {
                _logger.LogTrace("No old events to clean up");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform event cleanup");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Event cleanup service stopping...");
        await base.StopAsync(cancellationToken);
    }
} 