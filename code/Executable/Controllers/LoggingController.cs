using Infrastructure.Logging;
using Microsoft.AspNetCore.Mvc;
using Serilog.Events;

namespace Executable.Controllers;

[ApiController]
[Route("api/[controller]/level")]
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
    [HttpGet]
    public IActionResult GetLogLevel()
    {
        return Ok(new { level = _loggingConfigManager.GetLogLevel().ToString() });
    }
    
    /// <summary>
    /// Sets the global log level
    /// </summary>
    /// <param name="logLevel">New log level</param>
    [HttpPut]
    public async Task<IActionResult> SetLogLevel([FromBody] LogEventLevel logLevel)
    {
        await _loggingConfigManager.SetLogLevel(logLevel);
        
        _logger.LogInformation("Log level changed to {level}", logLevel);
              
        return Ok();
    }
}