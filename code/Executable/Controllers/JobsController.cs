using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Executable.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobManagementService _jobManagementService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IJobManagementService jobManagementService, ILogger<JobsController> logger)
    {
        _jobManagementService = jobManagementService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllJobs()
    {
        try
        {
            var result = await _jobManagementService.GetAllJobs();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all jobs");
            return StatusCode(500, "An error occurred while retrieving jobs");
        }
    }

    [HttpGet("{jobName}")]
    public async Task<IActionResult> GetJob(string jobName)
    {
        try
        {
            var jobInfo = await _jobManagementService.GetJob(jobName);
            if (jobInfo.Status == "Not Found")
            {
                return NotFound($"Job '{jobName}' not found");
            }
            return Ok(jobInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job {jobName}", jobName);
            return StatusCode(500, $"An error occurred while retrieving job '{jobName}'");
        }
    }

    [HttpPost("{jobName}/start")]
    public async Task<IActionResult> StartJob(string jobName, [FromQuery] string cronExpression = null)
    {
        try
        {
            var result = await _jobManagementService.StartJob(jobName, cronExpression);
            if (!result)
            {
                return BadRequest($"Failed to start job '{jobName}'");
            }
            return Ok(new { Message = $"Job '{jobName}' started successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting job {jobName}", jobName);
            return StatusCode(500, $"An error occurred while starting job '{jobName}'");
        }
    }

    [HttpPost("{jobName}/stop")]
    public async Task<IActionResult> StopJob(string jobName)
    {
        try
        {
            var result = await _jobManagementService.StopJob(jobName);
            if (!result)
            {
                return BadRequest($"Failed to stop job '{jobName}'");
            }
            return Ok(new { Message = $"Job '{jobName}' stopped successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping job {jobName}", jobName);
            return StatusCode(500, $"An error occurred while stopping job '{jobName}'");
        }
    }

    [HttpPost("{jobName}/pause")]
    public async Task<IActionResult> PauseJob(string jobName)
    {
        try
        {
            var result = await _jobManagementService.PauseJob(jobName);
            if (!result)
            {
                return BadRequest($"Failed to pause job '{jobName}'");
            }
            return Ok(new { Message = $"Job '{jobName}' paused successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing job {jobName}", jobName);
            return StatusCode(500, $"An error occurred while pausing job '{jobName}'");
        }
    }

    [HttpPost("{jobName}/resume")]
    public async Task<IActionResult> ResumeJob(string jobName)
    {
        try
        {
            var result = await _jobManagementService.ResumeJob(jobName);
            if (!result)
            {
                return BadRequest($"Failed to resume job '{jobName}'");
            }
            return Ok(new { Message = $"Job '{jobName}' resumed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming job {jobName}", jobName);
            return StatusCode(500, $"An error occurred while resuming job '{jobName}'");
        }
    }

    [HttpPut("{jobName}/schedule")]
    public async Task<IActionResult> UpdateJobSchedule(string jobName, [FromQuery] string cronExpression)
    {
        if (string.IsNullOrEmpty(cronExpression))
        {
            return BadRequest("Cron expression is required");
        }

        try
        {
            var result = await _jobManagementService.UpdateJobSchedule(jobName, cronExpression);
            if (!result)
            {
                return BadRequest($"Failed to update schedule for job '{jobName}'");
            }
            return Ok(new { Message = $"Job '{jobName}' schedule updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job {jobName} schedule", jobName);
            return StatusCode(500, $"An error occurred while updating schedule for job '{jobName}'");
        }
    }
}
