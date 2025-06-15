using Common.Configuration;
using Data.Models.Configuration.Arr;
using Data.Models.Configuration.DownloadCleaner;
using Data.Models.Configuration.General;
using Data.Models.Configuration.QueueCleaner;
using Data;
using Data.Enums;
using Infrastructure.Logging;
using Infrastructure.Models;
using Infrastructure.Services.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Executable.DTOs;

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
    public async Task<IActionResult> CreateDownloadClientConfig([FromBody] CreateDownloadClientDto newClient)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Validate the configuration
            newClient.Validate();
            
            // Create the full config from the DTO
            var clientConfig = new DownloadClientConfig
            {
                Enabled = newClient.Enabled,
                Name = newClient.Name,
                TypeName = newClient.TypeName,
                Type = newClient.Type,
                Host = newClient.Host,
                Username = newClient.Username,
                Password = newClient.Password,
                UrlBase = newClient.UrlBase
            };
            
            // Add the new client to the database
            _dataContext.DownloadClients.Add(clientConfig);
            await _dataContext.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetDownloadClientConfig), new { id = clientConfig.Id }, clientConfig);
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
            var config = await _dataContext.ArrConfigs
                .Include(x => x.Instances)
                .AsNoTracking()
                .FirstAsync(x => x.Type == InstanceType.Sonarr);
            return Ok(config.Adapt<ArrConfigDto>());
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
            var config = await _dataContext.ArrConfigs
                .Include(x => x.Instances)
                .AsNoTracking()
                .FirstAsync(x => x.Type == InstanceType.Radarr);
            return Ok(config.Adapt<ArrConfigDto>());
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
            var config = await _dataContext.ArrConfigs
                .AsNoTracking()
                .FirstAsync(x => x.Type == InstanceType.Lidarr);
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

            // Apply updates from DTO, excluding the ID property to avoid EF key modification error
            var config = new TypeAdapterConfig();
            config.NewConfig<QueueCleanerConfig, QueueCleanerConfig>()
                .Ignore(dest => dest.Id);
            
            newConfig.Adapt(oldConfig, config);

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

            // Apply updates from DTO, excluding the ID property to avoid EF key modification error
            var config = new TypeAdapterConfig();
            config.NewConfig<DownloadCleanerConfig, DownloadCleanerConfig>()
                .Ignore(dest => dest.Id);
            
            newConfig.Adapt(oldConfig, config);

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

            // Apply updates from DTO, excluding the ID property to avoid EF key modification error
            var config = new TypeAdapterConfig();
            config.NewConfig<GeneralConfig, GeneralConfig>()
                .Ignore(dest => dest.Id);
            
            newConfig.Adapt(oldConfig, config);

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
    public async Task<IActionResult> UpdateSonarrConfig([FromBody] UpdateSonarrConfigDto newConfigDto)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Get existing config
            var config = await _dataContext.ArrConfigs
                .FirstAsync(x => x.Type == InstanceType.Sonarr);

            config.Enabled = newConfigDto.Enabled;
            config.FailedImportMaxStrikes = newConfigDto.FailedImportMaxStrikes;

            // Validate the configuration
            config.Validate();

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
    public async Task<IActionResult> UpdateRadarrConfig([FromBody] UpdateRadarrConfigDto newConfigDto)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Get existing config
            var config = await _dataContext.ArrConfigs
                .FirstAsync(x => x.Type == InstanceType.Radarr);

            config.Enabled = newConfigDto.Enabled;
            config.FailedImportMaxStrikes = newConfigDto.FailedImportMaxStrikes;

            // Validate the configuration
            config.Validate();

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
    public async Task<IActionResult> UpdateLidarrConfig([FromBody] UpdateLidarrConfigDto newConfigDto)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Get existing config
            // var oldConfig = await _dataContext.LidarrConfigs
            //     .Include(x => x.Instances)
            //     .FirstAsync();
            //
            // // Create new config with updated basic settings only (instances managed separately)
            // var updatedConfig = new LidarrConfig
            // {
            //     Id = oldConfig.Id, // Keep the existing ID
            //     Enabled = newConfigDto.Enabled,
            //     FailedImportMaxStrikes = newConfigDto.FailedImportMaxStrikes,
            //     Instances = oldConfig.Instances // Keep existing instances unchanged
            // };
            //
            // // Validate the configuration
            // updatedConfig.Validate();
            //
            // // Update the existing entity using Mapster, excluding the ID
            // var config = new TypeAdapterConfig();
            // config.NewConfig<LidarrConfig, LidarrConfig>()
            //     .Ignore(dest => dest.Id)
            //     .Ignore(dest => dest.Instances); // Don't update instances here
            //
            // updatedConfig.Adapt(oldConfig, config);

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

    [HttpPost("sonarr/instances")]
    public async Task<IActionResult> CreateSonarrInstance([FromBody] CreateArrInstanceDto newInstance)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Get the Sonarr config to add the instance to
            var config = await _dataContext.ArrConfigs
                .FirstAsync(x => x.Type == InstanceType.Sonarr);

            // Create the new instance
            var instance = new ArrInstance
            {
                Name = newInstance.Name,
                Url = new Uri(newInstance.Url),
                ApiKey = newInstance.ApiKey,
                ArrConfigId = config.Id,
            };
            
            // Add to the config's instances collection
            // config.Instances.Add(instance);
            await _dataContext.ArrInstances.AddAsync(instance);
            // Save changes
            await _dataContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSonarrConfig), new { id = instance.Id }, instance.Adapt<ArrInstanceDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Sonarr instance");
            return StatusCode(500, "Failed to create Sonarr instance");
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpPut("sonarr/instances/{id}")]
    public async Task<IActionResult> UpdateSonarrInstance(Guid id, [FromBody] CreateArrInstanceDto updatedInstance)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Get the Sonarr config and find the instance
            var config = await _dataContext.ArrConfigs
                .Include(c => c.Instances)
                .FirstAsync(x => x.Type == InstanceType.Sonarr);

            var instance = config.Instances.FirstOrDefault(i => i.Id == id);
            if (instance == null)
            {
                return NotFound($"Sonarr instance with ID {id} not found");
            }

            // Update the instance properties
            instance.Name = updatedInstance.Name;
            instance.Url = new Uri(updatedInstance.Url);
            instance.ApiKey = updatedInstance.ApiKey;

            await _dataContext.SaveChangesAsync();

            return Ok(instance.Adapt<ArrInstanceDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Sonarr instance with ID {Id}", id);
            return StatusCode(500, "Failed to update Sonarr instance");
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpDelete("sonarr/instances/{id}")]
    public async Task<IActionResult> DeleteSonarrInstance(Guid id)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Get the Sonarr config and find the instance
            var config = await _dataContext.ArrConfigs
                .Include(c => c.Instances)
                .FirstAsync(x => x.Type == InstanceType.Sonarr);

            var instance = config.Instances.FirstOrDefault(i => i.Id == id);
            if (instance == null)
            {
                return NotFound($"Sonarr instance with ID {id} not found");
            }

            // Remove the instance
            config.Instances.Remove(instance);
            await _dataContext.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Sonarr instance with ID {Id}", id);
            return StatusCode(500, "Failed to delete Sonarr instance");
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpPost("radarr/instances")]
    public async Task<IActionResult> CreateRadarrInstance([FromBody] CreateArrInstanceDto newInstance)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Get the Radarr config to add the instance to
            var config = await _dataContext.ArrConfigs
                .FirstAsync(x => x.Type == InstanceType.Radarr);

            // Create the new instance
            var instance = new ArrInstance
            {
                Name = newInstance.Name,
                Url = new Uri(newInstance.Url),
                ApiKey = newInstance.ApiKey,
                ArrConfigId = config.Id,
            };
            
            // Add to the config's instances collection
            await _dataContext.ArrInstances.AddAsync(instance);
            // Save changes
            await _dataContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRadarrConfig), new { id = instance.Id }, instance.Adapt<ArrInstanceDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Radarr instance");
            return StatusCode(500, "Failed to create Radarr instance");
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpPut("radarr/instances/{id}")]
    public async Task<IActionResult> UpdateRadarrInstance(Guid id, [FromBody] CreateArrInstanceDto updatedInstance)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Get the Radarr config and find the instance
            var config = await _dataContext.ArrConfigs
                .Include(c => c.Instances)
                .FirstAsync(x => x.Type == InstanceType.Radarr);

            var instance = config.Instances.FirstOrDefault(i => i.Id == id);
            if (instance == null)
            {
                return NotFound($"Radarr instance with ID {id} not found");
            }

            // Update the instance properties
            instance.Name = updatedInstance.Name;
            instance.Url = new Uri(updatedInstance.Url);
            instance.ApiKey = updatedInstance.ApiKey;

            await _dataContext.SaveChangesAsync();

            return Ok(instance.Adapt<ArrInstanceDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Radarr instance with ID {Id}", id);
            return StatusCode(500, "Failed to update Radarr instance");
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpDelete("radarr/instances/{id}")]
    public async Task<IActionResult> DeleteRadarrInstance(Guid id)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Get the Radarr config and find the instance
            var config = await _dataContext.ArrConfigs
                .Include(c => c.Instances)
                .FirstAsync(x => x.Type == InstanceType.Radarr);
            
            var instance = config.Instances.FirstOrDefault(i => i.Id == id);
            if (instance == null)
            {
                return NotFound($"Radarr instance with ID {id} not found");
            }
            
            // Remove the instance
            config.Instances.Remove(instance);
            await _dataContext.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Radarr instance with ID {Id}", id);
            return StatusCode(500, "Failed to delete Radarr instance");
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpPost("lidarr/instances")]
    public async Task<IActionResult> CreateLidarrInstance([FromBody] CreateArrInstanceDto newInstance)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Get the Lidarr config to add the instance to
            var config = await _dataContext.ArrConfigs
                .Include(c => c.Instances)
                .FirstAsync(x => x.Type == InstanceType.Lidarr);

            // Create the new instance
            var instance = new ArrInstance
            {
                Name = newInstance.Name,
                Url = new Uri(newInstance.Url),
                ApiKey = newInstance.ApiKey,
                ArrConfigId = config.Id,
                ArrConfig = config // Set the navigation property
            };

            // Add to the config
            config.Instances.Add(instance);
            await _dataContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLidarrConfig), new { id = instance.Id }, instance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Lidarr instance");
            return StatusCode(500, "Failed to create Lidarr instance");
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpPut("lidarr/instances/{id}")]
    public async Task<IActionResult> UpdateLidarrInstance(Guid id, [FromBody] CreateArrInstanceDto updatedInstance)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Get the Lidarr config and find the instance
            var config = await _dataContext.ArrConfigs
                .Include(c => c.Instances)
                .FirstAsync(x => x.Type == InstanceType.Lidarr);

            var instance = config.Instances.FirstOrDefault(i => i.Id == id);
            if (instance == null)
            {
                return NotFound($"Lidarr instance with ID {id} not found");
            }

            // Update the instance properties
            instance.Name = updatedInstance.Name;
            instance.Url = new Uri(updatedInstance.Url);
            instance.ApiKey = updatedInstance.ApiKey;

            await _dataContext.SaveChangesAsync();

            return Ok(instance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Lidarr instance with ID {Id}", id);
            return StatusCode(500, "Failed to update Lidarr instance");
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }

    [HttpDelete("lidarr/instances/{id}")]
    public async Task<IActionResult> DeleteLidarrInstance(Guid id)
    {
        await DataContext.Lock.WaitAsync();
        try
        {
            // Get the Lidarr config and find the instance
            var config = await _dataContext.ArrConfigs
                .Include(c => c.Instances)
                .FirstAsync(x => x.Type == InstanceType.Lidarr);

            var instance = config.Instances.FirstOrDefault(i => i.Id == id);
            if (instance == null)
            {
                return NotFound($"Lidarr instance with ID {id} not found");
            }

            // Remove the instance
            config.Instances.Remove(instance);
            await _dataContext.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Lidarr instance with ID {Id}", id);
            return StatusCode(500, "Failed to delete Lidarr instance");
        }
        finally
        {
            DataContext.Lock.Release();
        }
    }
}