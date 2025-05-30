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
using Infrastructure.Services;
using Mapster;
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
            var dto = config.Adapt<QueueCleanerConfigDto>();
            return Ok(dto);
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
            var dto = config.Adapt<ContentBlockerConfigDto>();
            return Ok(dto);
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
            var dto = config.Adapt<DownloadCleanerConfigDto>();
            return Ok(dto);
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
            var dto = config.Adapt<DownloadClientConfigDto>();
            return Ok(dto);
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
            var dto = config.Adapt<IgnoredDownloadsConfigDto>();
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving IgnoredDownloads configuration");
            return StatusCode(500, "An error occurred while retrieving IgnoredDownloads configuration");
        }
    }
    
    [HttpGet("general")]
    public async Task<IActionResult> GetGeneralConfig()
    {
        try
        {
            var config = await _configManager.GetConfigurationAsync<GeneralConfig>();
            var dto = config.Adapt<GeneralConfigDto>();
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving General configuration");
            return StatusCode(500, "An error occurred while retrieving General configuration");
        }
    }
    
    [HttpGet("sonarr")]
    public async Task<IActionResult> GetSonarrConfig()
    {
        try
        {
            var config = await _configManager.GetConfigurationAsync<SonarrConfig>();
            var dto = config.Adapt<SonarrConfigDto>();
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Sonarr configuration");
            return StatusCode(500, "An error occurred while retrieving Sonarr configuration");
        }
    }
    
    [HttpGet("radarr")]
    public async Task<IActionResult> GetRadarrConfig()
    {
        try
        {
            var config = await _configManager.GetConfigurationAsync<RadarrConfig>();
            var dto = config.Adapt<RadarrConfigDto>();
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Radarr configuration");
            return StatusCode(500, "An error occurred while retrieving Radarr configuration");
        }
    }
    
    [HttpGet("lidarr")]
    public async Task<IActionResult> GetLidarrConfig()
    {
        try
        {
            var config = await _configManager.GetConfigurationAsync<LidarrConfig>();
            var dto = config.Adapt<LidarrConfigDto>();
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Lidarr configuration");
            return StatusCode(500, "An error occurred while retrieving Lidarr configuration");
        }
    }
    
    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotificationsConfig()
    {
        try
        {
            var config = await _configManager.GetConfigurationAsync<NotificationsConfig>();
            var dto = config.Adapt<NotificationsConfigDto>();
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Notifications configuration");
            return StatusCode(500, "An error occurred while retrieving Notifications configuration");
        }
    }

    [HttpPut("queue_cleaner")]
    public async Task<IActionResult> UpdateQueueCleanerConfig([FromBody] QueueCleanerConfigUpdateDto dto)
    {
        try
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
    public async Task<IActionResult> UpdateContentBlockerConfig([FromBody] ContentBlockerConfigUpdateDto dto)
    {
        try
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
    public async Task<IActionResult> UpdateDownloadCleanerConfig([FromBody] DownloadCleanerConfigUpdateDto dto)
    {
        try
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
    public async Task<IActionResult> UpdateDownloadClientConfig([FromBody] DownloadClientConfigUpdateDto dto)
    {
        try
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
    public async Task<IActionResult> UpdateIgnoredDownloadsConfig([FromBody] IgnoredDownloadsConfigUpdateDto dto)
    {
        try
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
            
            _logger.LogInformation("IgnoredDownloads configuration updated successfully");
            return Ok(new { Message = "IgnoredDownloads configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating IgnoredDownloads configuration");
            return StatusCode(500, "An error occurred while updating IgnoredDownloads configuration");
        }
    }
    
    [HttpPut("general")]
    public async Task<IActionResult> UpdateGeneralConfig([FromBody] GeneralConfigUpdateDto dto)
    {
        try
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
            
            _logger.LogInformation("General configuration updated successfully");
            return Ok(new { Message = "General configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating General configuration");
            return StatusCode(500, "An error occurred while updating General configuration");
        }
    }
    
    [HttpPut("sonarr")]
    public async Task<IActionResult> UpdateSonarrConfig([FromBody] SonarrConfigUpdateDto dto)
    {
        try
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
            
            _logger.LogInformation("Sonarr configuration updated successfully");
            return Ok(new { Message = "Sonarr configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Sonarr configuration");
            return StatusCode(500, "An error occurred while updating Sonarr configuration");
        }
    }
    
    [HttpPut("radarr")]
    public async Task<IActionResult> UpdateRadarrConfig([FromBody] RadarrConfigUpdateDto dto)
    {
        try
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
            
            _logger.LogInformation("Radarr configuration updated successfully");
            return Ok(new { Message = "Radarr configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Radarr configuration");
            return StatusCode(500, "An error occurred while updating Radarr configuration");
        }
    }
    
    [HttpPut("lidarr")]
    public async Task<IActionResult> UpdateLidarrConfig([FromBody] LidarrConfigUpdateDto dto)
    {
        try
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
            
            _logger.LogInformation("Lidarr configuration updated successfully");
            return Ok(new { Message = "Lidarr configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Lidarr configuration");
            return StatusCode(500, "An error occurred while updating Lidarr configuration");
        }
    }
    
    [HttpPut("notifications")]
    public async Task<IActionResult> UpdateNotificationsConfig([FromBody] NotificationsConfigUpdateDto dto)
    {
        try
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
            
            _logger.LogInformation("Notifications configuration updated successfully");
            return Ok(new { Message = "Notifications configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Notifications configuration");
            return StatusCode(500, "An error occurred while updating Notifications configuration");
        }
    }
    
    // TODO add missing configs
    // TODO do not return passwords
}
