using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Logging;

/// <summary>
/// SignalR hub for streaming log messages to connected clients
/// </summary>
public class LogHub : Hub
{
    private readonly SignalRLogSink _logSink;
    
    public LogHub(SignalRLogSink logSink)
    {
        _logSink = logSink;
    }
    
    /// <summary>
    /// Allows a client to request all recent logs from the buffer
    /// </summary>
    public async Task RequestRecentLogs()
    {
        foreach (var logEvent in _logSink.GetRecentLogs())
        {
            await Clients.Caller.SendAsync("ReceiveLog", logEvent);
        }
    }
}
