using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace Infrastructure.Logging;

/// <summary>
/// A Serilog sink that sends log events to SignalR clients
/// </summary>
public class SignalRLogSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _buffer = new();
    private readonly int _bufferSize;
    private readonly ConcurrentQueue<object> _logBuffer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SignalRLogSink> _logger;
    private IHubContext<LogHub> _hubContext;
    private volatile bool _isInitialized;
    
    public SignalRLogSink(IServiceProvider serviceProvider, ILogger<SignalRLogSink> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _bufferSize = 100;
        _logBuffer = new ConcurrentQueue<object>();
    }
    
    /// <summary>
    /// Processes and emits a log event to SignalR clients
    /// </summary>
    /// <param name="logEvent">The log event to emit</param>
    public void Emit(LogEvent logEvent)
    {
        if (!_isInitialized)
        {
            // Buffer the event until we can initialize
            _buffer.Enqueue(logEvent);
            
            // Try to initialize if not already done
            TryInitialize();
            return;
        }
        
        try
        {
            var logData = new
            {
                Timestamp = logEvent.Timestamp.DateTime,
                Level = logEvent.Level.ToString(),
                Message = logEvent.RenderMessage(),
                Exception = logEvent.Exception?.ToString(),
                Category = GetPropertyValue(logEvent, "Category", "SYSTEM"),
                JobName = GetPropertyValue(logEvent, "JobName"),
                InstanceName = GetPropertyValue(logEvent, "InstanceName")
            };
            
            // Add to buffer for new clients
            AddToBuffer(logData);
            
            // Send to connected clients
            _ = _hubContext.Clients.All.SendAsync("ReceiveLog", logData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send log event via SignalR");
        }
    }
    
    /// <summary>
    /// Gets the buffer of recent logs
    /// </summary>
    public IEnumerable<object> GetRecentLogs()
    {
        return _logBuffer.ToArray();
    }
    
    /// <summary>
    /// Initialize the SignalR hub context
    /// </summary>
    public void Initialize()
    {
        TryInitialize();
    }
    
    private void TryInitialize()
    {
        if (_isInitialized)
            return;
            
        try
        {
            _hubContext = _serviceProvider.GetRequiredService<IHubContext<LogHub>>();
            _isInitialized = true;
            
            // Process any buffered events
            ProcessBufferedEvents();
            
            _logger.LogInformation("SignalR log sink initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SignalR log sink initialization deferred - hub context not available yet");
        }
    }
    
    private void ProcessBufferedEvents()
    {
        while (_buffer.TryDequeue(out var logEvent))
        {
            Emit(logEvent);
        }
    }
    
    private void AddToBuffer(object logData)
    {
        _logBuffer.Enqueue(logData);
        
        // Trim buffer if it exceeds the limit
        while (_logBuffer.Count > _bufferSize && _logBuffer.TryDequeue(out _)) { }
    }
    
    private string GetPropertyValue(LogEvent logEvent, string propertyName, string defaultValue = null)
    {
        if (logEvent.Properties.TryGetValue(propertyName, out var value))
        {
            return value.ToString().Trim('\"');
        }
        
        return defaultValue;
    }
}
