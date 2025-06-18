using Cleanuparr.Domain.Enums;
using Cleanuparr.Infrastructure.Events;
using Cleanuparr.Infrastructure.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Cleanuparr.Infrastructure.Logging;

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
        return;
        while (true)
        {
            using var _ = LogContext.PushProperty(LogProperties.Category,
                random.Next(0, 100) > 50 ? InstanceType.Sonarr.ToString() : InstanceType.Radarr.ToString());
            try
            {
                
                await _eventPublisher.PublishAsync(
                    random.Next(0, 100) > 50 ? EventType.DownloadCleaned : EventType.StalledStrike,
                    "This is a very long message to test how it all looks in the frontend. This is just gibberish, but helps us figure out how the layout should be to display messages properly.",
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
                _logger.LogError(exception, "This is a very long message to test how it all looks in the frontend. This is just gibberish, but helps us figure out how the layout should be to display messages properly.");
            }
            
            await Task.Delay(10000, stoppingToken);
        }
    }
}
