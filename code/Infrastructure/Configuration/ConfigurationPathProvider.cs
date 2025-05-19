namespace Infrastructure.Configuration;

/// <summary>
/// Provides the appropriate configuration path based on the runtime environment.
/// Uses '/config' for Docker containers and a relative 'config' path for normal environments.
/// </summary>
public static class ConfigurationPathProvider
{
    private static string? _configPath;
    private static string? _settingsPath;
    
    static ConfigurationPathProvider()
    {
        try
        {
            string configPath = InitializeConfigPath();
            
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }
            
            string settingsPath = InitializeSettingsPath();
            
            if (!Directory.Exists(settingsPath))
            {
                Directory.CreateDirectory(settingsPath);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create configuration directories: {ex.Message}", ex);
        }
    }

    private static string InitializeConfigPath()
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
            _configPath = "config";
        }

        return _configPath;
    }

    private static string InitializeSettingsPath()
    {
        if (string.IsNullOrEmpty(_settingsPath))
        {
            string configPath = _configPath ?? InitializeConfigPath();
            _settingsPath = Path.Combine(configPath, "settings");
        }

        return _settingsPath;
    }
    
    public static string GetConfigPath()
    {
        return _configPath ?? InitializeConfigPath();
    }

    public static string GetSettingsPath()
    {
        return _settingsPath ?? InitializeConfigPath();
    }
}
