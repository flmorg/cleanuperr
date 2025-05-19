using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;

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
        var deferredSink = Log.Logger as ILoggerPropertyEnricher;
        
        if (deferredSink != null)
        {
            var sinks = deferredSink.GetType()
                .GetProperty("Sinks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(deferredSink) as System.Collections.IEnumerable;
            
            if (sinks != null)
            {
                foreach (var sink in sinks)
                {
                    if (sink is DeferredSignalRSink deferredSignalRSink)
                    {
                        deferredSignalRSink.Initialize(_serviceProvider);
                    }
                }
            }
        }
        
        // We only need to run this once at startup
        return Task.CompletedTask;
    }
}
