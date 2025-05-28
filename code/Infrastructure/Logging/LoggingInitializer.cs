using Data.Enums;
using Infrastructure.Events;
using Infrastructure.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Infrastructure.Logging;

// TODO remove
public class LoggingInitializer : BackgroundService
{
    private readonly ILogger<LoggingInitializer> _logger;
    private readonly EventPublisher _eventPublisher;
    private readonly Random random = new();
    
    public LoggingInitializer(ILogger<LoggingInitializer> logger, EventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            using var _ = LogContext.PushProperty(LogProperties.Category,
                random.Next(0, 100) > 50 ? InstanceType.Sonarr.ToString() : InstanceType.Radarr.ToString());
            try
            {
                
                await _eventPublisher.PublishAsync(
                    random.Next(0, 100) > 50 ? EventType.DownloadCleaned : EventType.StalledStrike,
                    "test",
                    EventSeverity.Important,
                    data: new { Hash = "hash", Name = "name", StrikeCount = "1", Type = "stalled" });
                throw new Exception("test exception");
            }
            catch (Exception exception)
            {
                _logger.LogCritical("test critical");
                _logger.LogTrace("test trace");
                _logger.LogDebug("test debug");
                _logger.LogWarning("test warn");
                _logger.LogError(exception, "test");
            }
            
            await Task.Delay(10000, stoppingToken);
        }
    }
}
