using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace Common.Configuration.Logging;

public class LoggingConfig : IConfig
{
    public const string SectionName = "Logging";
    
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;
    
    public bool Enhanced { get; set; }
    
    public FileLogConfig? File { get; set; }
    
    public SignalRLogConfig? SignalR { get; set; }
    
    public void Validate()
    {
    }
}