using Microsoft.Extensions.Hosting;

namespace Infrastructure.Configuration;

/// <summary>
/// Provides the appropriate configuration path based on the runtime environment.
/// Uses '/config' for Docker containers and a relative 'config' path for normal environments.
/// </summary>
public class ConfigurationPathProvider
{
    private readonly string _configPath;
    private readonly string _settingsPath;
    
    public ConfigurationPathProvider(IHostEnvironment environment)
    {
        // Check if running in Docker container
        bool isInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        
        if (isInContainer)
        {
            // Use absolute path for Docker
            _configPath = "/config";
        }
        else
        {
            // Use path relative to app for normal environment
            _configPath = Path.Combine(environment.ContentRootPath, "config");
        }
        
        // Create settings path as a subdirectory
        _settingsPath = Path.Combine(_configPath, "settings");
        
        // Ensure directories exist
        EnsureDirectoriesExist();
    }
    
    public string GetConfigPath() => _configPath;
    
    public string GetSettingsPath() => _settingsPath;
    
    private void EnsureDirectoriesExist()
    {
        try
        {
            if (!Directory.Exists(_configPath))
            {
                Directory.CreateDirectory(_configPath);
            }
            
            if (!Directory.Exists(_settingsPath))
            {
                Directory.CreateDirectory(_settingsPath);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create configuration directories: {ex.Message}", ex);
        }
    }
}
