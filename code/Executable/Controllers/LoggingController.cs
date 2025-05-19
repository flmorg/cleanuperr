using Infrastructure.Logging;
using Microsoft.AspNetCore.Mvc;
using Serilog.Events;

namespace Executable.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoggingController : ControllerBase
{
    private readonly LoggingConfigManager _loggingConfigManager;
    private readonly ILogger<LoggingController> _logger;
    
    public LoggingController(LoggingConfigManager loggingConfigManager, ILogger<LoggingController> logger)
    {
        _loggingConfigManager = loggingConfigManager;
        _logger = logger;
    }
    
    /// <summary>
    /// Gets the current global log level
    /// </summary>
    /// <returns>Current log level</returns>
    [HttpGet("level")]
    public IActionResult GetLogLevel()
    {
        return Ok(new { level = _loggingConfigManager.GetLogLevel().ToString() });
    }
    
    /// <summary>
    /// Sets the global log level
    /// </summary>
    /// <param name="request">Log level request containing the new level</param>
    /// <returns>Result with the new log level</returns>
    [HttpPut("level")]
    public async Task<IActionResult> SetLogLevel([FromBody] LogLevelRequest request)
    {
        if (!Enum.TryParse<LogEventLevel>(request.Level, true, out var logLevel))
        {
            return BadRequest(new 
            { 
                error = "Invalid log level", 
                validLevels = Enum.GetNames<LogEventLevel>() 
            });
        }
        
        await _loggingConfigManager.SetLogLevel(logLevel);
        
        // Log at the new level to confirm it's working
        _logger.WithCategory(LoggingCategoryConstants.System)
              .LogInformation("Log level changed to {Level}", logLevel);
              
        return Ok(new { level = logLevel.ToString() });
    }
    
    /// <summary>
    /// Get a list of valid log levels
    /// </summary>
    /// <returns>All valid log level values</returns>
    [HttpGet("levels")]
    public IActionResult GetValidLogLevels()
    {
        return Ok(new 
        { 
            levels = Enum.GetNames<LogEventLevel>() 
        });
    }
}

/// <summary>
/// Request model for changing log level
/// </summary>
public class LogLevelRequest
{
    public string Level { get; set; }
}
