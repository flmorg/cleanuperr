using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.IgnoredDownloads;
using Common.Configuration.QueueCleaner;
using Infrastructure.Configuration;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Executable.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly ILogger<ConfigurationController> _logger;
    private readonly IConfigManager _configManager;

    public ConfigurationController(
        ILogger<ConfigurationController> logger,
        IConfigManager configManager)
    {
        _logger = logger;
        _configManager = configManager;
    }

    [HttpGet("queue_cleaner")]
    public async Task<IActionResult> GetQueueCleanerConfig()
    {
        try
        {
            var config = await _configManager.GetConfigurationAsync<QueueCleanerConfig>();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving QueueCleaner configuration");
            return StatusCode(500, "An error occurred while retrieving QueueCleaner configuration");
        }
    }

    [HttpGet("content_blocker")]
    public async Task<IActionResult> GetContentBlockerConfig()
    {
        try
        {
            var config = await _configManager.GetConfigurationAsync<ContentBlockerConfig>();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ContentBlocker configuration");
            return StatusCode(500, "An error occurred while retrieving ContentBlocker configuration");
        }
    }

    [HttpGet("download_cleaner")]
    public async Task<IActionResult> GetDownloadCleanerConfig()
    {
        try
        {
            var config = await _configManager.GetConfigurationAsync<DownloadCleanerConfig>();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving DownloadCleaner configuration");
            return StatusCode(500, "An error occurred while retrieving DownloadCleaner configuration");
        }
    }
    
    [HttpGet("download_client")]
    public async Task<IActionResult> GetDownloadClientConfig()
    {
        try
        {
            var config = await _configManager.GetConfigurationAsync<DownloadClientConfig>();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving DownloadClient configuration");
            return StatusCode(500, "An error occurred while retrieving DownloadClient configuration");
        }
    }
    
    [HttpGet("ignored_downloads")]
    public async Task<IActionResult> GetIgnoredDownloadsConfig()
    {
        try
        {
            var config = await _configManager.GetConfigurationAsync<IgnoredDownloadsConfig>();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving IgnoredDownloads configuration");
            return StatusCode(500, "An error occurred while retrieving IgnoredDownloads configuration");
        }
    }

    [HttpPut("queue_cleaner")]
    public async Task<IActionResult> UpdateQueueCleanerConfig([FromBody] QueueCleanerConfig config)
    {
        try
        {
            // Validate the configuration
            config.Validate();
            
            // Persist the configuration
            var result = await _configManager.SaveConfigurationAsync(config);
            if (!result)
            {
                return StatusCode(500, "Failed to save QueueCleaner configuration");
            }
            
            _logger.LogInformation("QueueCleaner configuration updated successfully");
            return Ok(new { Message = "QueueCleaner configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating QueueCleaner configuration");
            return StatusCode(500, "An error occurred while updating QueueCleaner configuration");
        }
    }

    [HttpPut("content_blocker")]
    public async Task<IActionResult> UpdateContentBlockerConfig([FromBody] ContentBlockerConfig config)
    {
        try
        {
            // Validate the configuration
            config.Validate();
            
            // Persist the configuration
            var result = await _configManager.SaveConfigurationAsync(config);
            if (!result)
            {
                return StatusCode(500, "Failed to save ContentBlocker configuration");
            }
            
            _logger.LogInformation("ContentBlocker configuration updated successfully");
            return Ok(new { Message = "ContentBlocker configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ContentBlocker configuration");
            return StatusCode(500, "An error occurred while updating ContentBlocker configuration");
        }
    }

    [HttpPut("download_cleaner")]
    public async Task<IActionResult> UpdateDownloadCleanerConfig([FromBody] DownloadCleanerConfig config)
    {
        try
        {
            // Validate the configuration
            config.Validate();
            
            // Persist the configuration
            var result = await _configManager.SaveConfigurationAsync(config);
            if (!result)
            {
                return StatusCode(500, "Failed to save DownloadCleaner configuration");
            }
            
            _logger.LogInformation("DownloadCleaner configuration updated successfully");
            return Ok(new { Message = "DownloadCleaner configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DownloadCleaner configuration");
            return StatusCode(500, "An error occurred while updating DownloadCleaner configuration");
        }
    }
    
    [HttpPut("download_client")]
    public async Task<IActionResult> UpdateDownloadClientConfig([FromBody] DownloadClientConfig config)
    {
        try
        {
            // Validate the configuration
            config.Validate();
            
            // Persist the configuration
            var result = await _configManager.SaveConfigurationAsync(config);
            if (!result)
            {
                return StatusCode(500, "Failed to save DownloadClient configuration");
            }
            
            _logger.LogInformation("DownloadClient configuration updated successfully");
            return Ok(new { Message = "DownloadClient configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DownloadClient configuration: {Message}", ex.Message);
            return BadRequest(new { Error = ex.Message });
        }
    }
    
    [HttpPut("ignored_downloads")]
    public async Task<IActionResult> UpdateIgnoredDownloadsConfig([FromBody] IgnoredDownloadsConfig config)
    {
        try
        {
            // Persist the configuration
            var result = await _configManager.SaveConfigurationAsync(config);
            if (!result)
            {
                return StatusCode(500, "Failed to save IgnoredDownloads configuration");
            }
            
            _logger.LogInformation("IgnoredDownloads configuration updated successfully");
            return Ok(new { Message = "IgnoredDownloads configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating IgnoredDownloads configuration");
            return StatusCode(500, "An error occurred while updating IgnoredDownloads configuration");
        }
    }
    
    // TODO add missing configs
    // TODO do not return passwords
}
