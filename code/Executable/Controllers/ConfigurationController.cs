using Common.Configuration;
using Common.Configuration.Arr;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.General;
using Common.Configuration.QueueCleaner;
using Data;
using Infrastructure.Logging;
using Infrastructure.Models;
using Infrastructure.Services.Interfaces;
using Infrastructure.Verticals.ContentBlocker;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace Executable.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly ILogger<ConfigurationController> _logger;
    private readonly DataContext _dataContext;
    private readonly LoggingConfigManager _loggingConfigManager;
    private readonly IJobManagementService _jobManagementService;

    public ConfigurationController(
        ILogger<ConfigurationController> logger,
        DataContext dataContext,
        LoggingConfigManager loggingConfigManager,
        IJobManagementService jobManagementService
    )
    {
        _logger = logger;
        _dataContext = dataContext;
        _loggingConfigManager = loggingConfigManager;
        _jobManagementService = jobManagementService;
    }

    [HttpGet("queue_cleaner")]
    public async Task<IActionResult> GetQueueCleanerConfig()
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            var config = await _dataContext.QueueCleanerConfigs
                .AsNoTracking()
                .FirstAsync();
            return Ok(config);
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpGet("download_cleaner")]
    public async Task<IActionResult> GetDownloadCleanerConfig()
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            var config = await _dataContext.DownloadCleanerConfigs
                .AsNoTracking()
                .FirstAsync();
            return Ok(config);
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpGet("download_client")]
    public async Task<IActionResult> GetDownloadClientConfig()
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            var clients = await _dataContext.DownloadClients
                .AsNoTracking()
                .ToListAsync();
            
            // Return in the expected format with clients wrapper
            var config = new { clients = clients };
            return Ok(config);
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }
    
    [HttpPost("download_client")]
    public async Task<IActionResult> CreateDownloadClientConfig([FromBody] DownloadClientConfig newClient)
    {
        if (newClient == null)
        {
            return BadRequest("Invalid download client data");
        }
        
        await DataContext.Lock.WaitAsync();
        try
        {
            // Validate the configuration
            newClient.Validate();
            
            // Add the new client to the database
            _dataContext.DownloadClients.Add(newClient);
            await _dataContext.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetDownloadClientConfig), new { id = newClient.Id }, newClient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create download client");
            return StatusCode(500, "Failed to create download client configuration");
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }
    
    [HttpPut("download_client/{id}")]
    public async Task<IActionResult> UpdateDownloadClientConfig(Guid id, [FromBody] DownloadClientConfig updatedClient)
    {
        if (updatedClient == null)
        {
            return BadRequest("Invalid download client data");
        }
        
        await DataContext.Lock.WaitAsync();
        try
        {
            // Find the existing download client
            var existingClient = await _dataContext.DownloadClients
                .FirstOrDefaultAsync(c => c.Id == id);
                
            if (existingClient == null)
            {
                return NotFound($"Download client with ID {id} not found");
            }
            
            // Ensure the ID in the path matches the entity being updated
            updatedClient = updatedClient with { Id = id };
            
            // Apply updates from DTO
            updatedClient.Adapt(existingClient);
            
            // Persist the configuration
            await _dataContext.SaveChangesAsync();
            
            return Ok(existingClient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update download client with ID {Id}", id);
            return StatusCode(500, "Failed to update download client configuration");
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }
    
    [HttpDelete("download_client/{id}")]
    public async Task<IActionResult> DeleteDownloadClientConfig(Guid id)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Find the existing download client
            var existingClient = await _dataContext.DownloadClients
                .FirstOrDefaultAsync(c => c.Id == id);
                
            if (existingClient == null)
            {
                return NotFound($"Download client with ID {id} not found");
            }
            
            // Remove the client from the database
            _dataContext.DownloadClients.Remove(existingClient);
            await _dataContext.SaveChangesAsync();
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete download client with ID {Id}", id);
            return StatusCode(500, "Failed to delete download client configuration");
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpGet("general")]
    public async Task<IActionResult> GetGeneralConfig()
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            var config = await _dataContext.GeneralConfigs
                .AsNoTracking()
                .FirstAsync();
            return Ok(config);
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpGet("sonarr")]
    public async Task<IActionResult> GetSonarrConfig()
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            var config = await _dataContext.SonarrConfigs
                .AsNoTracking()
                .FirstAsync();
            return Ok(config);
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpGet("radarr")]
    public async Task<IActionResult> GetRadarrConfig()
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            var config = await _dataContext.RadarrConfigs
                .AsNoTracking()
                .FirstAsync();
            return Ok(config);
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpGet("lidarr")]
    public async Task<IActionResult> GetLidarrConfig()
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            var config = await _dataContext.LidarrConfigs
                .AsNoTracking()
                .FirstAsync();
            return Ok(config);
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotificationsConfig()
    {
        // TODO get all notification configs
        await DataContext.Lock.WaitAsync();
        try
        {
            // var config = await _dataContext.NotificationsConfigs
            //     .AsNoTracking()
            //     .FirstAsync();
            // return Ok(config);
            return null; // Placeholder for future implementation
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpPut("queue_cleaner")]
    public async Task<IActionResult> UpdateQueueCleanerConfig([FromBody] QueueCleanerConfig newConfig)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Validate the configuration
            newConfig.Validate();

            // Get existing config
            var oldConfig = await _dataContext.QueueCleanerConfigs
                .FirstAsync();

            // Apply updates from DTO
            newConfig.Adapt(oldConfig);

            // Persist the configuration
            await _dataContext.SaveChangesAsync();

            // Update the scheduler based on configuration changes
            await UpdateJobSchedule(oldConfig, JobType.QueueCleaner);

            return Ok(new { Message = "QueueCleaner configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save QueueCleaner configuration");
            return StatusCode(500, "Failed to save QueueCleaner configuration");
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpPut("download_cleaner")]
    public async Task<IActionResult> UpdateDownloadCleanerConfig([FromBody] DownloadCleanerConfig newConfig)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Validate the configuration
            newConfig.Validate();

            // Get existing config
            var oldConfig = await _dataContext.DownloadCleanerConfigs
                .Include(x => x.Categories)
                .FirstAsync();

            // Apply updates from DTO
            newConfig.Adapt(oldConfig);

            // Persist the configuration
            await _dataContext.SaveChangesAsync();

            // Update the scheduler based on configuration changes
            await UpdateJobSchedule(oldConfig, JobType.DownloadCleaner);

            return Ok(new { Message = "DownloadCleaner configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save DownloadCleaner configuration");
            return StatusCode(500, "Failed to save DownloadCleaner configuration");
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }
    
    [HttpPut("general")]
    public async Task<IActionResult> UpdateGeneralConfig([FromBody] GeneralConfig newConfig)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Validate the configuration
            newConfig.Validate();

            // Get existing config
            var oldConfig = await _dataContext.GeneralConfigs
                .FirstAsync();

            // Apply updates from DTO
            newConfig.Adapt(oldConfig);

            // Persist the configuration
            await _dataContext.SaveChangesAsync();

            // Set the logging level based on the new configuration
            _loggingConfigManager.SetLogLevel(newConfig.LogLevel);

            return Ok(new { Message = "General configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save General configuration");
            return StatusCode(500, "Failed to save General configuration");
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpPut("sonarr")]
    public async Task<IActionResult> UpdateSonarrConfig([FromBody] SonarrConfig newConfig)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Validate the configuration
            newConfig.Validate();

            // Get existing config
            var oldConfig = await _dataContext.SonarrConfigs
                .FirstAsync();

            // Apply updates from DTO
            newConfig.Adapt(oldConfig);

            // Persist the configuration
            await _dataContext.SaveChangesAsync();

            return Ok(new { Message = "Sonarr configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Sonarr configuration");
            return StatusCode(500, "Failed to save Sonarr configuration");
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpPut("radarr")]
    public async Task<IActionResult> UpdateRadarrConfig([FromBody] RadarrConfig newConfig)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Validate the configuration
            newConfig.Validate();

            // Get existing config
            var oldConfig = await _dataContext.RadarrConfigs
                .FirstAsync();

            // Apply updates from DTO
            newConfig.Adapt(oldConfig);

            // Persist the configuration
            await _dataContext.SaveChangesAsync();

            return Ok(new { Message = "Radarr configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Radarr configuration");
            return StatusCode(500, "Failed to save Radarr configuration");
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpPut("lidarr")]
    public async Task<IActionResult> UpdateLidarrConfig([FromBody] LidarrConfig newConfig)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Validate the configuration
            newConfig.Validate();

            // Get existing config
            var oldConfig = await _dataContext.LidarrConfigs
                .FirstAsync();

            // Apply updates from DTO
            newConfig.Adapt(oldConfig);

            // Persist the configuration
            await _dataContext.SaveChangesAsync();

            return Ok(new { Message = "Lidarr configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Lidarr configuration");
            return StatusCode(500, "Failed to save Lidarr configuration");
        }
        finally
        {
            DataContext.Lock.Release();
        }
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
}