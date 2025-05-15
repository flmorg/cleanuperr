using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Infrastructure.Configuration;

namespace Executable.Controllers;

[ApiController]
[Route("api/config-files")]
public class ConfigFilesController : ControllerBase
{
    private readonly ILogger<ConfigFilesController> _logger;
    private readonly IConfigManager _configManager;

    public ConfigFilesController(
        ILogger<ConfigFilesController> logger,
        IConfigManager configManager)
    {
        _logger = logger;
        _configManager = configManager;
    }

    /// <summary>
    /// Lists all available configuration files
    /// </summary>
    [HttpGet]
    public IActionResult GetAllConfigFiles()
    {
        try
        {
            var files = _configManager.ListConfigurationFiles().ToList();
            return Ok(new { Files = files, Count = files.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing configuration files");
            return StatusCode(500, "An error occurred while listing configuration files");
        }
    }

    /// <summary>
    /// Gets the content of a specific configuration file
    /// </summary>
    [HttpGet("{fileName}")]
    public async Task<IActionResult> GetConfigFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName) || !fileName.EndsWith(".json"))
        {
            fileName = $"{fileName}.json";
        }

        try
        {
            // Read as dynamic to support any JSON structure
            var config = await _configManager.GetConfigurationAsync<object>(fileName);
            
            if (config == null)
            {
                return NotFound($"Configuration file '{fileName}' not found");
            }
            
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading configuration file {fileName}", fileName);
            return StatusCode(500, $"An error occurred while reading configuration file '{fileName}'");
        }
    }

    /// <summary>
    /// Creates or updates a configuration file
    /// </summary>
    [HttpPut("{fileName}")]
    public async Task<IActionResult> SaveConfigFile(string fileName, [FromBody] JsonElement content)
    {
        if (string.IsNullOrEmpty(fileName) || !fileName.EndsWith(".json"))
        {
            fileName = $"{fileName}.json";
        }

        try
        {
            // Convert the JsonElement to an object
            var configObject = JsonSerializer.Deserialize<object>(content.GetRawText());
            
            if (configObject == null)
            {
                return BadRequest("Invalid JSON content");
            }
            
            var result = await _configManager.SaveConfigurationAsync(fileName, configObject);
            
            if (!result)
            {
                return StatusCode(500, $"Failed to save configuration file '{fileName}'");
            }
            
            return Ok(new { Message = $"Configuration file '{fileName}' saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration file {fileName}", fileName);
            return StatusCode(500, $"An error occurred while saving configuration file '{fileName}'");
        }
    }

    /// <summary>
    /// Deletes a configuration file
    /// </summary>
    [HttpDelete("{fileName}")]
    public async Task<IActionResult> DeleteConfigFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName) || !fileName.EndsWith(".json"))
        {
            fileName = $"{fileName}.json";
        }

        try
        {
            var result = await _configManager.DeleteConfigurationAsync(fileName);
            
            if (!result)
            {
                return StatusCode(500, $"Failed to delete configuration file '{fileName}'");
            }
            
            return Ok(new { Message = $"Configuration file '{fileName}' deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration file {fileName}", fileName);
            return StatusCode(500, $"An error occurred while deleting configuration file '{fileName}'");
        }
    }

    /// <summary>
    /// Updates a specific property in a configuration file
    /// </summary>
    [HttpPatch("{fileName}")]
    public async Task<IActionResult> UpdateConfigProperty(
        string fileName, 
        [FromQuery] string propertyPath, 
        [FromBody] JsonElement value)
    {
        if (string.IsNullOrEmpty(fileName) || !fileName.EndsWith(".json"))
        {
            fileName = $"{fileName}.json";
        }

        if (string.IsNullOrEmpty(propertyPath))
        {
            return BadRequest("Property path is required");
        }

        try
        {
            // Convert the JsonElement to an object 
            var valueObject = JsonSerializer.Deserialize<object>(value.GetRawText());
            
            if (valueObject == null)
            {
                return BadRequest("Invalid value");
            }
            
            var result = await _configManager.UpdateConfigurationPropertyAsync(fileName, propertyPath, valueObject);
            
            if (!result)
            {
                return StatusCode(500, $"Failed to update property '{propertyPath}' in configuration file '{fileName}'");
            }
            
            return Ok(new { Message = $"Property '{propertyPath}' in configuration file '{fileName}' updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating property {propertyPath} in configuration file {fileName}", propertyPath, fileName);
            return StatusCode(500, $"An error occurred while updating property '{propertyPath}' in configuration file '{fileName}'");
        }
    }

    /// <summary>
    /// Gets the Sonarr configuration
    /// </summary>
    [HttpGet("sonarr")]
    public async Task<IActionResult> GetSonarrConfig()
    {
        try
        {
            var config = await _configManager.GetSonarrConfigAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Sonarr configuration");
            return StatusCode(500, "An error occurred while getting Sonarr configuration");
        }
    }

    /// <summary>
    /// Gets the Radarr configuration
    /// </summary>
    [HttpGet("radarr")]
    public async Task<IActionResult> GetRadarrConfig()
    {
        try
        {
            var config = await _configManager.GetRadarrConfigAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Radarr configuration");
            return StatusCode(500, "An error occurred while getting Radarr configuration");
        }
    }

    /// <summary>
    /// Gets the Lidarr configuration
    /// </summary>
    [HttpGet("lidarr")]
    public async Task<IActionResult> GetLidarrConfig()
    {
        try
        {
            var config = await _configManager.GetLidarrConfigAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Lidarr configuration");
            return StatusCode(500, "An error occurred while getting Lidarr configuration");
        }
    }

    /// <summary>
    /// Gets the ContentBlocker configuration
    /// </summary>
    [HttpGet("contentblocker")]
    public async Task<IActionResult> GetContentBlockerConfig()
    {
        try
        {
            var config = await _configManager.GetContentBlockerConfigAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ContentBlocker configuration");
            return StatusCode(500, "An error occurred while getting ContentBlocker configuration");
        }
    }

    /// <summary>
    /// Gets the QueueCleaner configuration
    /// </summary>
    [HttpGet("queuecleaner")]
    public async Task<IActionResult> GetQueueCleanerConfig()
    {
        try
        {
            var config = await _configManager.GetQueueCleanerConfigAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting QueueCleaner configuration");
            return StatusCode(500, "An error occurred while getting QueueCleaner configuration");
        }
    }

    /// <summary>
    /// Gets the DownloadCleaner configuration
    /// </summary>
    [HttpGet("downloadcleaner")]
    public async Task<IActionResult> GetDownloadCleanerConfig()
    {
        try
        {
            var config = await _configManager.GetDownloadCleanerConfigAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting DownloadCleaner configuration");
            return StatusCode(500, "An error occurred while getting DownloadCleaner configuration");
        }
    }

    /// <summary>
    /// Gets the DownloadClient configuration
    /// </summary>
    [HttpGet("downloadclient")]
    public async Task<IActionResult> GetDownloadClientConfig()
    {
        try
        {
            var config = await _configManager.GetDownloadClientConfigAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting DownloadClient configuration");
            return StatusCode(500, "An error occurred while getting DownloadClient configuration");
        }
    }
}
