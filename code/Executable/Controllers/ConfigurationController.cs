using Common.Configuration.Arr;
using Common.Configuration.ContentBlocker;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.DTOs.Arr;
using Common.Configuration.DTOs.ContentBlocker;
using Common.Configuration.DTOs.DownloadCleaner;
using Common.Configuration.DTOs.DownloadClient;
using Common.Configuration.DTOs.General;
using Common.Configuration.DTOs.IgnoredDownloads;
using Common.Configuration.DTOs.Notification;
using Common.Configuration.DTOs.QueueCleaner;
using Common.Configuration.General;
using Common.Configuration.IgnoredDownloads;
using Common.Configuration.Notification;
using Common.Configuration.QueueCleaner;
using Infrastructure.Configuration;
using Infrastructure.Models;
using Infrastructure.Services;
using Infrastructure.Services.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace Executable.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly ILogger<ConfigurationController> _logger;
    private readonly IConfigManager _configManager;
    private readonly IJobManagementService _jobManagementService;

    public ConfigurationController(
        ILogger<ConfigurationController> logger,
        IConfigManager configManager,
        IJobManagementService jobManagementService)
    {
        _logger = logger;
        _configManager = configManager;
        _jobManagementService = jobManagementService;
    }

    [HttpGet("queue_cleaner")]
    public async Task<IActionResult> GetQueueCleanerConfig()
    {
        var config = await _configManager.GetConfigurationAsync<QueueCleanerConfig>();
        var dto = config.Adapt<QueueCleanerConfigDto>();
        return Ok(dto);
    }

    [HttpGet("content_blocker")]
    public async Task<IActionResult> GetContentBlockerConfig()
    {
        var config = await _configManager.GetConfigurationAsync<ContentBlockerConfig>();
        var dto = config.Adapt<ContentBlockerConfigDto>();
        return Ok(dto);
    }

    [HttpGet("download_cleaner")]
    public async Task<IActionResult> GetDownloadCleanerConfig()
    {
        var config = await _configManager.GetConfigurationAsync<DownloadCleanerConfig>();
        var dto = config.Adapt<DownloadCleanerConfigDto>();
        return Ok(dto);
    }
    
    [HttpGet("download_client")]
    public async Task<IActionResult> GetDownloadClientConfig()
    {
        var config = await _configManager.GetConfigurationAsync<DownloadClientConfig>();
        var dto = config.Adapt<DownloadClientConfigDto>();
        return Ok(dto);
    }
    
    [HttpGet("ignored_downloads")]
    public async Task<IActionResult> GetIgnoredDownloadsConfig()
    {
        var config = await _configManager.GetConfigurationAsync<IgnoredDownloadsConfig>();
        var dto = config.Adapt<IgnoredDownloadsConfigDto>();
        return Ok(dto);
    }
    
    [HttpGet("general")]
    public async Task<IActionResult> GetGeneralConfig()
    {
        var config = await _configManager.GetConfigurationAsync<GeneralConfig>();
        var dto = config.Adapt<GeneralConfigDto>();
        return Ok(dto);
    }
    
    [HttpGet("sonarr")]
    public async Task<IActionResult> GetSonarrConfig()
    {
        var config = await _configManager.GetConfigurationAsync<SonarrConfig>();
        var dto = config.Adapt<SonarrConfigDto>();
        return Ok(dto);
    }
    
    [HttpGet("radarr")]
    public async Task<IActionResult> GetRadarrConfig()
    {
        var config = await _configManager.GetConfigurationAsync<RadarrConfig>();
        var dto = config.Adapt<RadarrConfigDto>();
        return Ok(dto);
    }
    
    [HttpGet("lidarr")]
    public async Task<IActionResult> GetLidarrConfig()
    {
        var config = await _configManager.GetConfigurationAsync<LidarrConfig>();
        var dto = config.Adapt<LidarrConfigDto>();
        return Ok(dto);
    }
    
    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotificationsConfig()
    {
        var config = await _configManager.GetConfigurationAsync<NotificationsConfig>();
        var dto = config.Adapt<NotificationsConfigDto>();
        return Ok(dto);
    }

    [HttpPut("queue_cleaner")]
    public async Task<IActionResult> UpdateQueueCleanerConfig([FromBody] QueueCleanerConfigUpdateDto dto)
    {
        // Get existing config
        var config = await _configManager.GetConfigurationAsync<QueueCleanerConfig>();
        
        // Apply updates from DTO
        dto.Adapt(config);
        
        // Validate the configuration
        config.Validate();
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(config);
        if (!result)
        {
            return StatusCode(500, "Failed to save QueueCleaner configuration");
        }
        
        // Update the scheduler based on configuration changes
        await UpdateQueueCleanerJobSchedule(config);
        
        return Ok(new { Message = "QueueCleaner configuration updated successfully" });
    }
    
    /// <summary>
    /// Updates the QueueCleaner job schedule based on configuration changes
    /// </summary>
    /// <param name="config">The QueueCleaner configuration</param>
    private async Task UpdateQueueCleanerJobSchedule(QueueCleanerConfig config)
    {
        if (config.Enabled)
        {
            // If the job is enabled, update its schedule with the configured cron expression
            _logger.LogInformation("QueueCleaner is enabled, updating job schedule with cron expression: {CronExpression}", config.CronExpression);
            
            // Create a Quartz job schedule with the cron expression
            // Note: This is using the raw cron expression, not creating a JobSchedule object
            // since QueueCleanerConfig already contains a cron expression
            await _jobManagementService.StartJob(JobType.QueueCleaner, null, config.CronExpression);
        }
        else
        {
            // If the job is disabled, stop it
            _logger.LogInformation("QueueCleaner is disabled, stopping the job");
            await _jobManagementService.StopJob(JobType.QueueCleaner);
        }
    }
    
    /// <summary>
    /// Updates the DownloadCleaner job schedule based on configuration changes
    /// </summary>
    /// <param name="config">The DownloadCleaner configuration</param>
    private async Task UpdateDownloadCleanerJobSchedule(DownloadCleanerConfig config)
    {
        if (config.Enabled)
        {
            // If the job is enabled, update its schedule with the configured cron expression
            _logger.LogInformation("DownloadCleaner is enabled, updating job schedule with cron expression: {CronExpression}", config.CronExpression);
            
            // Create a Quartz job schedule with the cron expression
            await _jobManagementService.StartJob(JobType.DownloadCleaner, null, config.CronExpression);
        }
        else
        {
            // If the job is disabled, stop it
            _logger.LogInformation("DownloadCleaner is disabled, stopping the job");
            await _jobManagementService.StopJob(JobType.DownloadCleaner);
        }
    }

    [HttpPut("content_blocker")]
    public async Task<IActionResult> UpdateContentBlockerConfig([FromBody] ContentBlockerConfigUpdateDto dto)
    {
        // Get existing config
        var config = await _configManager.GetConfigurationAsync<ContentBlockerConfig>();
        
        // Apply updates from DTO
        dto.Adapt(config);
        
        // Validate the configuration
        config.Validate();
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(config);
        if (!result)
        {
            return StatusCode(500, "Failed to save ContentBlocker configuration");
        }
        
        return Ok(new { Message = "ContentBlocker configuration updated successfully" });
    }

    [HttpPut("download_cleaner")]
    public async Task<IActionResult> UpdateDownloadCleanerConfig([FromBody] DownloadCleanerConfigUpdateDto dto)
    {
        // Get existing config
        var config = await _configManager.GetConfigurationAsync<DownloadCleanerConfig>();
        
        // Apply updates from DTO
        dto.Adapt(config);
        
        // Validate the configuration
        config.Validate();
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(config);
        if (!result)
        {
            return StatusCode(500, "Failed to save DownloadCleaner configuration");
        }
        
        // Update the scheduler based on configuration changes
        await UpdateDownloadCleanerJobSchedule(config);
        
        return Ok(new { Message = "DownloadCleaner configuration updated successfully" });
    }
    
    [HttpPut("download_client")]
    public async Task<IActionResult> UpdateDownloadClientConfig([FromBody] DownloadClientConfigUpdateDto dto)
    {
        // Get existing config to preserve sensitive data
        var config = await _configManager.GetConfigurationAsync<DownloadClientConfig>();
        
        // Apply updates from DTO, preserving sensitive data if not provided
        dto.Adapt(config);
        
        // Validate the configuration
        config.Validate();
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(config);
        if (!result)
        {
            return StatusCode(500, "Failed to save DownloadClient configuration");
        }
        
        return Ok(new { Message = "DownloadClient configuration updated successfully" });
    }
    
    [HttpPut("ignored_downloads")]
    public async Task<IActionResult> UpdateIgnoredDownloadsConfig([FromBody] IgnoredDownloadsConfigUpdateDto dto)
    {
        // Get existing config
        var config = await _configManager.GetConfigurationAsync<IgnoredDownloadsConfig>();
        
        // Apply updates from DTO
        dto.Adapt(config);
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(config);
        if (!result)
        {
            return StatusCode(500, "Failed to save IgnoredDownloads configuration");
        }
        
        return Ok(new { Message = "IgnoredDownloads configuration updated successfully" });
    }
    
    [HttpPut("general")]
    public async Task<IActionResult> UpdateGeneralConfig([FromBody] GeneralConfigUpdateDto dto)
    {
        // Get existing config to preserve sensitive data
        var config = await _configManager.GetConfigurationAsync<GeneralConfig>();
        
        // Apply updates from DTO, preserving sensitive data if not provided
        dto.Adapt(config);
        
        // Validate the configuration
        config.Validate();
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(config);
        if (!result)
        {
            return StatusCode(500, "Failed to save General configuration");
        }
        
        return Ok(new { Message = "General configuration updated successfully" });
    }
    
    [HttpPut("sonarr")]
    public async Task<IActionResult> UpdateSonarrConfig([FromBody] SonarrConfigUpdateDto dto)
    {
        // Get existing config to preserve sensitive data
        var config = await _configManager.GetConfigurationAsync<SonarrConfig>();
        
        // Apply updates from DTO, preserving sensitive data if not provided
        dto.Adapt(config);
        
        // Validate the configuration
        config.Validate();
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(config);
        if (!result)
        {
            return StatusCode(500, "Failed to save Sonarr configuration");
        }
        
        return Ok(new { Message = "Sonarr configuration updated successfully" });
    }
    
    [HttpPut("radarr")]
    public async Task<IActionResult> UpdateRadarrConfig([FromBody] RadarrConfigUpdateDto dto)
    {
        // Get existing config to preserve sensitive data
        var config = await _configManager.GetConfigurationAsync<RadarrConfig>();
        
        // Apply updates from DTO, preserving sensitive data if not provided
        dto.Adapt(config);
        
        // Validate the configuration
        config.Validate();
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(config);
        if (!result)
        {
            return StatusCode(500, "Failed to save Radarr configuration");
        }
        
        return Ok(new { Message = "Radarr configuration updated successfully" });
    }
    
    [HttpPut("lidarr")]
    public async Task<IActionResult> UpdateLidarrConfig([FromBody] LidarrConfigUpdateDto dto)
    {
        // Get existing config to preserve sensitive data
        var config = await _configManager.GetConfigurationAsync<LidarrConfig>();
        
        // Apply updates from DTO, preserving sensitive data if not provided
        dto.Adapt(config);
        
        // Validate the configuration
        config.Validate();
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(config);
        if (!result)
        {
            return StatusCode(500, "Failed to save Lidarr configuration");
        }
        
        return Ok(new { Message = "Lidarr configuration updated successfully" });
    }
    
    [HttpPut("notifications")]
    public async Task<IActionResult> UpdateNotificationsConfig([FromBody] NotificationsConfigUpdateDto dto)
    {
        // Get existing config to preserve sensitive data
        var config = await _configManager.GetConfigurationAsync<NotificationsConfig>();
        
        // Apply updates from DTO, preserving sensitive data if not provided
        dto.Adapt(config);
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(config);
        if (!result)
        {
            return StatusCode(500, "Failed to save Notifications configuration");
        }
        
        return Ok(new { Message = "Notifications configuration updated successfully" });
    }
}
