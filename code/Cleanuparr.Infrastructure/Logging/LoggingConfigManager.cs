using Cleanuparr.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace Cleanuparr.Infrastructure.Logging;

/// <summary>
/// Manages logging configuration and provides dynamic log level control
/// </summary>
public class LoggingConfigManager
{
    private readonly DataContext _dataContext;
    private readonly ILogger<LoggingConfigManager> _logger;

    private static LoggingLevelSwitch LevelSwitch = new();
    
    public LoggingConfigManager(DataContext dataContext, ILogger<LoggingConfigManager> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
        
        // Load settings from configuration
        LoadConfiguration();
    }
    
    /// <summary>
    /// Gets the level switch used to dynamically control log levels
    /// </summary>
    public LoggingLevelSwitch GetLevelSwitch() => LevelSwitch;
    
    /// <summary>
    /// Updates the global log level and persists the change to configuration
    /// </summary>
    /// <param name="level">The new log level</param>
    public void SetLogLevel(LogEventLevel level)
    {
        _logger.LogCritical("Setting global log level to {level}", level);
        
        // Change the level in the switch
        LevelSwitch.MinimumLevel = level;
    }
    
    /// <summary>
    /// Loads logging settings from configuration
    /// </summary>
    private void LoadConfiguration()
    {
        try
        {
            var config = _dataContext.GeneralConfigs
                .AsNoTracking()
                .First();
            LevelSwitch.MinimumLevel = config.LogLevel;
        }
        catch (Exception ex)
        {
            // Just log and continue with defaults
            _logger.LogError(ex, "Failed to load logging configuration, using defaults");
        }
    }
}
