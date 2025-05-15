using Common.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Services;

public interface IConfigurationService
{
    Task<bool> UpdateConfigurationAsync<T>(string sectionName, T configSection) where T : class, IConfig;
    Task<T?> GetConfigurationAsync<T>(string sectionName) where T : class, IConfig;
    Task<bool> RefreshConfigurationAsync();
}

public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _configFilePath;

    public ConfigurationService(
        ILogger<ConfigurationService> logger,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _logger = logger;
        _configuration = configuration;
        
        // Find primary configuration file
        var currentDirectory = environment.ContentRootPath;
        _configFilePath = Path.Combine(currentDirectory, "appsettings.json");
        
        if (!File.Exists(_configFilePath))
        {
            _logger.LogWarning("Configuration file not found at: {path}", _configFilePath);
            _configFilePath = Path.Combine(currentDirectory, "appsettings.Development.json");
            
            if (!File.Exists(_configFilePath))
            {
                _logger.LogError("No configuration file found");
                throw new FileNotFoundException("Configuration file not found");
            }
        }
        
        _logger.LogInformation("Using configuration file: {path}", _configFilePath);
    }

    public async Task<bool> UpdateConfigurationAsync<T>(string sectionName, T configSection) where T : class, IConfig
    {
        try
        {
            // Read existing configuration
            var json = await File.ReadAllTextAsync(_configFilePath);
            var jsonObject = JsonNode.Parse(json)?.AsObject() 
                ?? throw new InvalidOperationException("Failed to parse configuration file");
            
            // Create JsonObject from config section
            var configJson = JsonSerializer.Serialize(configSection);
            var configObject = JsonNode.Parse(configJson)?.AsObject()
                ?? throw new InvalidOperationException("Failed to serialize configuration");
            
            // Update or add the section
            jsonObject[sectionName] = configObject;
            
            // Save back to file
            var options = new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = jsonObject.ToJsonString(options);
            
            // Create backup
            var backupPath = $"{_configFilePath}.bak";
            await File.WriteAllTextAsync(backupPath, json);
            
            // Write updated configuration
            await File.WriteAllTextAsync(_configFilePath, updatedJson);
            
            // Refresh configuration
            await RefreshConfigurationAsync();
            
            _logger.LogInformation("Configuration section {section} updated successfully", sectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration section {section}", sectionName);
            return false;
        }
    }

    public async Task<T?> GetConfigurationAsync<T>(string sectionName) where T : class, IConfig
    {
        try
        {
            var json = await File.ReadAllTextAsync(_configFilePath);
            var jsonObject = JsonNode.Parse(json)?.AsObject();
            
            if (jsonObject == null || !jsonObject.ContainsKey(sectionName))
            {
                _logger.LogWarning("Section {section} not found in configuration", sectionName);
                return null;
            }
            
            var sectionObject = jsonObject[sectionName]?.ToJsonString();
            if (sectionObject == null)
            {
                return null;
            }
            
            return JsonSerializer.Deserialize<T>(sectionObject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration section {section}", sectionName);
            return null;
        }
    }

    public Task<bool> RefreshConfigurationAsync()
    {
        try
        {
            if (_configuration is IConfigurationRoot configRoot)
            {
                configRoot.Reload();
                _logger.LogInformation("Configuration reloaded");
                return Task.FromResult(true);
            }
            
            _logger.LogWarning("Unable to reload configuration: IConfigurationRoot not available");
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading configuration");
            return Task.FromResult(false);
        }
    }
}
