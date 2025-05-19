using System.Collections;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Infrastructure.Logging;

/// <summary>
/// A background service that initializes deferred logging components after startup
/// </summary>
public class LoggingInitializer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    
    public LoggingInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Find and initialize any deferred sinks
        var deferredSink = Log.Logger;

        if (deferredSink.GetType()
                .GetProperty("Sinks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(deferredSink) is IEnumerable sinks)
        {
            foreach (var sink in sinks)
            {
                if (sink is DeferredSignalRSink deferredSignalRSink)
                {
                    deferredSignalRSink.Initialize(_serviceProvider);
                }
            }
        }

        // We only need to run this once at startup
        return Task.CompletedTask;
    }
}
