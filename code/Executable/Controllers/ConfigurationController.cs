using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.QueueCleaner;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Executable.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly ILogger<ConfigurationController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<QueueCleanerConfig> _queueCleanerConfig;
    private readonly IOptionsMonitor<ContentBlockerConfig> _contentBlockerConfig;
    private readonly IOptionsMonitor<DownloadCleanerConfig> _downloadCleanerConfig;
    private readonly IConfigurationService _configService;

    public ConfigurationController(
        ILogger<ConfigurationController> logger,
        IConfiguration configuration,
        IOptionsMonitor<QueueCleanerConfig> queueCleanerConfig,
        IOptionsMonitor<ContentBlockerConfig> contentBlockerConfig,
        IOptionsMonitor<DownloadCleanerConfig> downloadCleanerConfig,
        IConfigurationService configService)
    {
        _logger = logger;
        _configuration = configuration;
        _queueCleanerConfig = queueCleanerConfig;
        _contentBlockerConfig = contentBlockerConfig;
        _downloadCleanerConfig = downloadCleanerConfig;
        _configService = configService;
    }

    [HttpGet("queuecleaner")]
    public IActionResult GetQueueCleanerConfig()
    {
        try
        {
            return Ok(_queueCleanerConfig.CurrentValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving QueueCleaner configuration");
            return StatusCode(500, "An error occurred while retrieving QueueCleaner configuration");
        }
    }

    [HttpGet("contentblocker")]
    public IActionResult GetContentBlockerConfig()
    {
        try
        {
            return Ok(_contentBlockerConfig.CurrentValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ContentBlocker configuration");
            return StatusCode(500, "An error occurred while retrieving ContentBlocker configuration");
        }
    }

    [HttpGet("downloadcleaner")]
    public IActionResult GetDownloadCleanerConfig()
    {
        try
        {
            return Ok(_downloadCleanerConfig.CurrentValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving DownloadCleaner configuration");
            return StatusCode(500, "An error occurred while retrieving DownloadCleaner configuration");
        }
    }

    [HttpPut("queuecleaner")]
    public async Task<IActionResult> UpdateQueueCleanerConfig([FromBody] QueueCleanerConfig config)
    {
        try
        {
            // Validate the configuration
            config.Validate();
            
            // Persist the configuration
            var result = await _configService.UpdateConfigurationAsync(QueueCleanerConfig.SectionName, config);
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

    [HttpPut("contentblocker")]
    public async Task<IActionResult> UpdateContentBlockerConfig([FromBody] ContentBlockerConfig config)
    {
        try
        {
            // Validate the configuration
            config.Validate();
            
            // Persist the configuration
            var result = await _configService.UpdateConfigurationAsync(ContentBlockerConfig.SectionName, config);
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

    [HttpPut("downloadcleaner")]
    public async Task<IActionResult> UpdateDownloadCleanerConfig([FromBody] DownloadCleanerConfig config)
    {
        try
        {
            // Validate the configuration
            config.Validate();
            
            // Persist the configuration
            var result = await _configService.UpdateConfigurationAsync(DownloadCleanerConfig.SectionName, config);
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
}
