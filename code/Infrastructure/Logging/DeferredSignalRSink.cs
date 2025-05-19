using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;

namespace Infrastructure.Logging;

/// <summary>
/// A Serilog sink that buffers events until the SignalR infrastructure is available
/// </summary>
public class DeferredSignalRSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _buffer = new();
    private volatile bool _isInitialized = false;
    private ILogEventSink _signalRSink;

    public void Emit(LogEvent logEvent)
    {
        if (!_isInitialized)
        {
            // Buffer the event until we can initialize
            _buffer.Enqueue(logEvent.Copy());
        }
        else
        {
            // Pass to the actual sink
            _signalRSink?.Emit(logEvent);
        }
    }

    /// <summary>
    /// Initialize the actual SignalR sink
    /// </summary>
    /// <param name="serviceProvider">The DI service provider</param>
    public void Initialize(IServiceProvider serviceProvider)
    {
        if (_isInitialized)
            return;

        try
        {
            // Create the actual sink when the hub context is available
            var hubContext = serviceProvider.GetRequiredService<IHubContext<LogHub>>();
            var logBuffer = serviceProvider.GetRequiredService<LogBuffer>();
            _signalRSink = new SignalRSink(hubContext, logBuffer);

            // Process buffered events
            while (_buffer.TryDequeue(out var logEvent))
            {
                _signalRSink.Emit(logEvent);
            }

            _isInitialized = true;
        }
        catch 
        {
            // Failed to initialize - will try again later
        }
    }
}
