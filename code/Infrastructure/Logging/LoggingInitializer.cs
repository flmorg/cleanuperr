using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Infrastructure.Logging;

/// <summary>
/// A background service that initializes deferred logging components after startup
/// </summary>
public class LoggingInitializer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LoggingInitializer> _logger;
    
    public LoggingInitializer(IServiceProvider serviceProvider, ILogger<LoggingInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                throw new Exception("eroare e ceva ce multa lume n-are, ye");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "test");
            }
            
            await Task.Delay(30000, stoppingToken);
        }
        
        // We only need to run this once at startup
    }
}
