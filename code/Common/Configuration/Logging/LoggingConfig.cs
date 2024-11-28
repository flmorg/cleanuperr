using Serilog.Events;

namespace Common.Configuration.Logging;

public class LoggingConfig : IConfig
{
    public const string SectionName = "Logging";
    
    public LogEventLevel LogLevel { get; set; }
    
    public FileLogConfig? File { get; set; }
    
    public void Validate()
    {
    }
}