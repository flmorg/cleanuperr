using Infrastructure.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Logging;

// TODO remove
public class LoggingInitializer : BackgroundService
{
    private readonly ILogger<LoggingInitializer> _logger;
    private readonly EventPublisher _eventPublisher;
    
    public LoggingInitializer(ILogger<LoggingInitializer> logger, EventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                await _eventPublisher.PublishAsync(
                    "strike",
                    "test",
                    "Item '{item}' has been struck {1} times for reason '{stalled}'",
                    severity: "Warning",
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
            
            await Task.Delay(30000, stoppingToken);
        }
    }
}
