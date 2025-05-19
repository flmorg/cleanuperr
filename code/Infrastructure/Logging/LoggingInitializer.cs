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
        try
        {
            // Short delay to ensure SignalR is fully initialized
            await Task.Delay(1000, stoppingToken);
            
            // Get the SignalRLogSink and initialize it
            _logger.LogDebug("Initializing SignalR logging");
            if (_serviceProvider.GetService<SignalRLogSink>() is { } sink)
            {
                sink.Initialize();
                _logger.LogInformation("SignalR logging initialized successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SignalR logging");
        }
        
        // We only need to run this once at startup
    }
}
