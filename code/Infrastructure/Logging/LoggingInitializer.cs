using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Logging;

// TODO remove
public class LoggingInitializer : BackgroundService
{
    private readonly ILogger<LoggingInitializer> _logger;
    
    public LoggingInitializer(ILogger<LoggingInitializer> logger)
    {
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                throw new Exception("test exception");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "test");
            }
            
            await Task.Delay(30000, stoppingToken);
        }
    }
}
