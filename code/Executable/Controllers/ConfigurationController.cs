using Common.Configuration;
using Common.Configuration.Arr;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.General;
using Common.Configuration.Notification;
using Common.Configuration.QueueCleaner;
using Infrastructure.Configuration;
using Infrastructure.Logging;
using Infrastructure.Models;
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
    private readonly LoggingConfigManager _loggingConfigManager;

    public ConfigurationController(
        ILogger<ConfigurationController> logger,
        IConfigManager configManager,
        IJobManagementService jobManagementService,
        LoggingConfigManager loggingConfigManager
    )
    {
        _logger = logger;
        _configManager = configManager;
        _jobManagementService = jobManagementService;
        _loggingConfigManager = loggingConfigManager;
    }

    [HttpGet("queue_cleaner")]
    public async Task<IActionResult> GetQueueCleanerConfig()
    {
        var config = await _configManager.GetConfigurationAsync<QueueCleanerConfig>();
        return Ok(config);
    }

    [HttpGet("download_cleaner")]
    public async Task<IActionResult> GetDownloadCleanerConfig()
    {
        var config = await _configManager.GetConfigurationAsync<DownloadCleanerConfig>();
        return Ok(config);
    }
    
    [HttpGet("download_client")]
    public async Task<IActionResult> GetDownloadClientConfig()
    {
        var config = await _configManager.GetConfigurationAsync<DownloadClientConfigs>();
        return Ok(config);
    }
    
    [HttpGet("general")]
    public async Task<IActionResult> GetGeneralConfig()
    {
        var config = await _configManager.GetConfigurationAsync<GeneralConfig>();
        return Ok(config);
    }
    
    [HttpGet("sonarr")]
    public async Task<IActionResult> GetSonarrConfig()
    {
        var config = await _configManager.GetConfigurationAsync<SonarrConfig>();
        return Ok(config);
    }
    
    [HttpGet("radarr")]
    public async Task<IActionResult> GetRadarrConfig()
    {
        var config = await _configManager.GetConfigurationAsync<RadarrConfig>();
        return Ok(config);
    }
    
    [HttpGet("lidarr")]
    public async Task<IActionResult> GetLidarrConfig()
    {
        var config = await _configManager.GetConfigurationAsync<LidarrConfig>();
        return Ok(config);
    }
    
    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotificationsConfig()
    {
        var config = await _configManager.GetConfigurationAsync<NotificationsConfig>();
        return Ok(config);
    }

    [HttpPut("queue_cleaner")]
    public async Task<IActionResult> UpdateQueueCleanerConfig([FromBody] QueueCleanerConfig dto)
    {
        // Get existing config
        var oldConfig = await _configManager.GetConfigurationAsync<QueueCleanerConfig>();
        
        // Apply updates from DTO, preserving sensitive data if not provided
        var newConfig = oldConfig.Adapt<QueueCleanerConfig>();
        newConfig = dto.Adapt(newConfig);
        
        // Validate the configuration
        newConfig.Validate();
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(newConfig);
        if (!result)
        {
            return StatusCode(500, "Failed to save QueueCleaner configuration");
        }
        
        // Update the scheduler based on configuration changes
        await UpdateJobSchedule(oldConfig, JobType.QueueCleaner);
        
        return Ok(new { Message = "QueueCleaner configuration updated successfully" });
    }
    
    /// <summary>
    /// Updates a job schedule based on configuration changes
    /// </summary>
    /// <param name="config">The job configuration</param>
    /// <param name="jobType">The type of job to update</param>
    private async Task UpdateJobSchedule(IJobConfig config, JobType jobType)
    {
        if (config.Enabled)
        {
            // Get the cron expression based on the specific config type
            if (!string.IsNullOrEmpty(config.CronExpression))
            {
                // If the job is enabled, update its schedule with the configured cron expression
                _logger.LogInformation("{name} is enabled, updating job schedule with cron expression: {CronExpression}", 
                    jobType.ToString(), config.CronExpression);
                
                _logger.LogCritical("This is a random test log");
                
                // Create a Quartz job schedule with the cron expression
                await _jobManagementService.StartJob(jobType, null, config.CronExpression);
            }
            else
            {
                _logger.LogWarning("{name} is enabled, but no cron expression was found in the configuration", jobType.ToString());
            }
            
            return;
        }
        
        // If the job is disabled, stop it
        _logger.LogInformation("{name} is disabled, stopping the job", jobType.ToString());
        await _jobManagementService.StopJob(jobType);
    }
    
    [HttpPut("content_blocker")]
    public async Task<IActionResult> UpdateContentBlockerConfig([FromBody] ContentBlockerConfig newConfig)
    {
        // Validate the configuration
        newConfig.Validate();
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(newConfig);
        if (!result)
        {
            return StatusCode(500, "Failed to save ContentBlocker configuration");
        }
        
        return Ok(new { Message = "ContentBlocker configuration updated successfully" });
    }

    [HttpPut("download_cleaner")]
    public async Task<IActionResult> UpdateDownloadCleanerConfig([FromBody] DownloadCleanerConfig dto)
    {
        // Get existing config
        var oldConfig = await _configManager.GetConfigurationAsync<DownloadCleanerConfig>();
        
        // Apply updates from DTO, preserving sensitive data if not provided
        var newConfig = oldConfig.Adapt<DownloadCleanerConfig>();
        newConfig = dto.Adapt(newConfig);
        
        // Validate the configuration
        newConfig.Validate();
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(newConfig);
        if (!result)
        {
            return StatusCode(500, "Failed to save DownloadCleaner configuration");
        }
        
        // Update the scheduler based on configuration changes
        await UpdateJobSchedule(oldConfig, JobType.DownloadCleaner);
        
        return Ok(new { Message = "DownloadCleaner configuration updated successfully" });
    }
    
    [HttpPut("download_client")]
    public async Task<IActionResult> UpdateDownloadClientConfig(DownloadClientConfigs newConfigs)
    {
        // Validate the configuration
        newConfigs.Validate();
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(newConfigs);
        if (!result)
        {
            return StatusCode(500, "Failed to save DownloadClient configuration");
        }
        
        return Ok(new { Message = "DownloadClient configuration updated successfully" });
    }
    
    [HttpPut("general")]
    public async Task<IActionResult> UpdateGeneralConfig([FromBody] GeneralConfig newConfig)
    {
        // Validate the configuration
        newConfig.Validate();
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(newConfig);
        if (!result)
        {
            return StatusCode(500, "Failed to save General configuration");
        }
        
        _loggingConfigManager.SetLogLevel(newConfig.LogLevel);

        return Ok(new { Message = "General configuration updated successfully" });
    }
    
    [HttpPut("sonarr")]
    public async Task<IActionResult> UpdateSonarrConfig([FromBody] SonarrConfig newConfig)
    {
        // Validate the configuration
        newConfig.Validate();
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(newConfig);
        if (!result)
        {
            return StatusCode(500, "Failed to save Sonarr configuration");
        }
        
        return Ok(new { Message = "Sonarr configuration updated successfully" });
    }
    
    [HttpPut("radarr")]
    public async Task<IActionResult> UpdateRadarrConfig([FromBody] RadarrConfig newConfig)
    {
        // Validate the configuration
        newConfig.Validate();
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(newConfig);
        if (!result)
        {
            return StatusCode(500, "Failed to save Radarr configuration");
        }
        
        return Ok(new { Message = "Radarr configuration updated successfully" });
    }
    
    [HttpPut("lidarr")]
    public async Task<IActionResult> UpdateLidarrConfig([FromBody] LidarrConfig newConfig)
    {
        // Validate the configuration
        newConfig.Validate();
        
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(newConfig);
        if (!result)
        {
            return StatusCode(500, "Failed to save Lidarr configuration");
        }
        
        return Ok(new { Message = "Lidarr configuration updated successfully" });
    }
    
    [HttpPut("notifications")]
    public async Task<IActionResult> UpdateNotificationsConfig([FromBody] NotificationsConfig newConfig)
    {
        // Persist the configuration
        var result = await _configManager.SaveConfigurationAsync(newConfig);
        if (!result)
        {
            return StatusCode(500, "Failed to save Notifications configuration");
        }
        
        return Ok(new { Message = "Notifications configuration updated successfully" });
    }
}
