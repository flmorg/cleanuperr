using Common.Configuration.General;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace Infrastructure.Logging;

/// <summary>
/// Manages logging configuration and provides dynamic log level control
/// </summary>
public class LoggingConfigManager
{
    private readonly IConfigManager _configManager;
    private readonly LoggingLevelSwitch _levelSwitch;
    private readonly ILogger<LoggingConfigManager> _logger;
    
    public LoggingConfigManager(IConfigManager configManager, ILogger<LoggingConfigManager> logger)
    {
        _configManager = configManager;
        _logger = logger;
        
        // Initialize with default level
        _levelSwitch = new LoggingLevelSwitch();
        
        // Load settings from configuration
        LoadConfiguration();
    }
    
    /// <summary>
    /// Gets the level switch used to dynamically control log levels
    /// </summary>
    public LoggingLevelSwitch GetLevelSwitch() => _levelSwitch;
    
    /// <summary>
    /// Updates the global log level and persists the change to configuration
    /// </summary>
    /// <param name="level">The new log level</param>
    public async Task SetLogLevel(LogEventLevel level)
    {
        // Change the level in the switch
        _levelSwitch.MinimumLevel = level;
        
        _logger.LogInformation("Setting global log level to {level}", level);
        
        // Update configuration
        try 
        {
            var config = await _configManager.GetConfigurationAsync<GeneralConfig>();

            config.LogLevel = level;
            
            // TODO use SetProp
            await _configManager.SaveConfigurationAsync(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update log level in configuration");
        }
    }
    
    /// <summary>
    /// Gets the current global log level
    /// </summary>
    public LogEventLevel GetLogLevel() => _levelSwitch.MinimumLevel;
    
    /// <summary>
    /// Loads logging settings from configuration
    /// </summary>
    private void LoadConfiguration()
    {
        try
        {
            var config = _configManager.GetConfiguration<GeneralConfig>();
            _levelSwitch.MinimumLevel = config.LogLevel;
        }
        catch (Exception ex)
        {
            // Just log and continue with defaults
            _logger.LogError(ex, "Failed to load logging configuration, using defaults");
        }
    }
}
