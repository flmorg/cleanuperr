namespace Common.Configuration.Logging;

/// <summary>
/// Configuration options for SignalR log streaming
/// </summary>
public class SignalRLogConfig : IConfig
{
    /// <summary>
    /// Whether SignalR logging is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Number of log entries to buffer for new connections
    /// </summary>
    public int BufferSize { get; set; } = 100;
    
    public void Validate()
    {
        if (BufferSize < 0)
        {
            BufferSize = 100;
        }
    }
}
