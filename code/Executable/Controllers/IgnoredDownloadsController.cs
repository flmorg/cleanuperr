using Common.Configuration.IgnoredDownloads;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Executable.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IgnoredDownloadsController : ControllerBase
{
    private readonly ILogger<IgnoredDownloadsController> _logger;
    private readonly IIgnoredDownloadsService _ignoredDownloadsService;

    public IgnoredDownloadsController(
        ILogger<IgnoredDownloadsController> logger,
        IIgnoredDownloadsService ignoredDownloadsService)
    {
        _logger = logger;
        _ignoredDownloadsService = ignoredDownloadsService;
    }

    /// <summary>
    /// Get all ignored downloads
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetIgnoredDownloads()
    {
        try
        {
            var ignoredDownloads = await _ignoredDownloadsService.GetIgnoredDownloadsAsync();
            return Ok(ignoredDownloads);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ignored downloads");
            return StatusCode(500, "An error occurred while retrieving ignored downloads");
        }
    }

    /// <summary>
    /// Add a new download ID to be ignored
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddIgnoredDownload([FromBody] string downloadId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(downloadId))
            {
                return BadRequest("Download ID cannot be empty");
            }

            var result = await _ignoredDownloadsService.AddIgnoredDownloadAsync(downloadId);
            if (!result)
            {
                return StatusCode(500, "Failed to add download ID to ignored list");
            }

            _logger.LogInformation("Added download ID to ignored list: {DownloadId}", downloadId);
            return Ok(new { Message = $"Download ID '{downloadId}' added to ignored list" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding download ID to ignored list");
            return StatusCode(500, "An error occurred while adding download ID to ignored list");
        }
    }

    /// <summary>
    /// Remove a download ID from the ignored list
    /// </summary>
    [HttpDelete("{downloadId}")]
    public async Task<IActionResult> RemoveIgnoredDownload(string downloadId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(downloadId))
            {
                return BadRequest("Download ID cannot be empty");
            }

            var result = await _ignoredDownloadsService.RemoveIgnoredDownloadAsync(downloadId);
            if (!result)
            {
                return StatusCode(500, "Failed to remove download ID from ignored list");
            }

            _logger.LogInformation("Removed download ID from ignored list: {DownloadId}", downloadId);
            return Ok(new { Message = $"Download ID '{downloadId}' removed from ignored list" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing download ID from ignored list");
            return StatusCode(500, "An error occurred while removing download ID from ignored list");
        }
    }

    /// <summary>
    /// Clear all ignored downloads
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> ClearIgnoredDownloads()
    {
        try
        {
            var result = await _ignoredDownloadsService.ClearIgnoredDownloadsAsync();
            if (!result)
            {
                return StatusCode(500, "Failed to clear ignored downloads list");
            }

            _logger.LogInformation("Cleared all ignored downloads");
            return Ok(new { Message = "All ignored downloads have been cleared" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing ignored downloads list");
            return StatusCode(500, "An error occurred while clearing ignored downloads list");
        }
    }
}
