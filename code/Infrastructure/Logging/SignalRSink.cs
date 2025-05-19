using Microsoft.AspNetCore.SignalR;
using Serilog.Core;
using Serilog.Events;

namespace Infrastructure.Logging;

/// <summary>
/// Serilog sink that forwards log events to SignalR clients
/// </summary>
public class SignalRSink : ILogEventSink
{
    private readonly IHubContext<LogHub> _hubContext;
    private readonly LogBuffer _logBuffer;
    
    public SignalRSink(IHubContext<LogHub> hubContext, LogBuffer logBuffer)
    {
        _hubContext = hubContext;
        _logBuffer = logBuffer;
    }
    
    /// <summary>
    /// Processes and emits a log event to SignalR clients
    /// </summary>
    /// <param name="logEvent">The log event to emit</param>
    public void Emit(LogEvent logEvent)
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
        
        _logBuffer.AddLog(logData);
        _hubContext.Clients.All.SendAsync("ReceiveLog", logData).GetAwaiter().GetResult();
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
