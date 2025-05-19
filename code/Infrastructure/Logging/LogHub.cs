using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Logging;

/// <summary>
/// SignalR hub for streaming log messages to connected clients
/// </summary>
public class LogHub : Hub
{
    private readonly LogBuffer _logBuffer;
    
    public LogHub(LogBuffer logBuffer)
    {
        _logBuffer = logBuffer;
    }
    
    /// <summary>
    /// Allows a client to request all recent logs from the buffer
    /// </summary>
    public async Task RequestRecentLogs()
    {
        foreach (var logEvent in _logBuffer.GetRecentLogs())
        {
            await Clients.Caller.SendAsync("ReceiveLog", logEvent);
        }
    }
}
