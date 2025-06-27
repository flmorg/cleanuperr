using System.Collections.Concurrent;

namespace Cleanuparr.Infrastructure.Logging;

/// <summary>
/// Maintains a buffer of recent log entries for newly connected clients
/// </summary>
public class LogBuffer
{
    private readonly ConcurrentQueue<object> _recentLogs;
    private readonly int _bufferSize;
    
    public LogBuffer(int bufferSize)
    {
        _bufferSize = Math.Max(10, bufferSize);
        _recentLogs = new ConcurrentQueue<object>();
    }
    
    /// <summary>
    /// Adds a log entry to the buffer
    /// </summary>
    /// <param name="logEvent">The log event to buffer</param>
    public void AddLog(object logEvent)
    {
        _recentLogs.Enqueue(logEvent);
        
        // Trim buffer if it exceeds size
        while (_recentLogs.Count > _bufferSize && _recentLogs.TryDequeue(out _)) { }
    }
    
    /// <summary>
    /// Gets all buffered log entries
    /// </summary>
    /// <returns>Collection of recent log events</returns>
    public IEnumerable<object> GetRecentLogs() => _recentLogs.ToArray();
}
