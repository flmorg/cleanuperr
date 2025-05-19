using Infrastructure.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Executable.Controllers;

/// <summary>
/// Sample controller demonstrating the use of the enhanced logging system.
/// This is for demonstration purposes only.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LoggingDemoController : ControllerBase
{
    private readonly ILogger<LoggingDemoController> _logger;

    public LoggingDemoController(ILogger<LoggingDemoController> logger)
    {
        _logger = logger;
    }

    [HttpGet("system")]
    public IActionResult LogSystemMessage()
    {
        // Using the Category extension
        _logger.WithCategory(LoggingCategoryConstants.System)
               .LogInformation("This is a system log message");
        
        return Ok("System log sent");
    }

    [HttpGet("api")]
    public IActionResult LogApiMessage()
    {
        _logger.WithCategory(LoggingCategoryConstants.Api)
               .LogInformation("This is an API log message");
        
        return Ok("API log sent");
    }

    [HttpGet("job")]
    public IActionResult LogJobMessage([FromQuery] string jobName = "CleanupJob")
    {
        _logger.WithCategory(LoggingCategoryConstants.Jobs)
               .WithJob(jobName)
               .LogInformation("This is a job-related log message");
        
        return Ok($"Job log sent for {jobName}");
    }

    [HttpGet("instance")]
    public IActionResult LogInstanceMessage([FromQuery] string instance = "Sonarr")
    {
        _logger.WithCategory(instance.ToUpper())
               .WithInstance(instance)
               .LogInformation("This is an instance-related log message");
        
        return Ok($"Instance log sent for {instance}");
    }

    [HttpGet("combined")]
    public IActionResult LogCombinedMessage(
        [FromQuery] string category = "JOBS",
        [FromQuery] string jobName = "ContentBlocker", 
        [FromQuery] string instance = "Sonarr")
    {
        _logger.WithCategory(category)
               .WithJob(jobName)
               .WithInstance(instance)
               .LogInformation("This log message combines category, job name, and instance");
        
        return Ok("Combined log sent");
    }

    [HttpGet("error")]
    public IActionResult LogErrorMessage()
    {
        try
        {
            // Simulate an error
            throw new InvalidOperationException("This is a test exception");
        }
        catch (Exception ex)
        {
            _logger.WithCategory(LoggingCategoryConstants.System)
                   .LogError(ex, "An error occurred during processing");
        }
        
        return Ok("Error log sent");
    }
}
