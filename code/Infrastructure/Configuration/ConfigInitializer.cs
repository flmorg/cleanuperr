using Microsoft.Extensions.Logging;

namespace Infrastructure.Configuration;

/// <summary>
/// Initializes default configuration files if they don't exist
/// </summary>
public class ConfigInitializer
{
    private readonly IConfigManager _configManager;
    private readonly ILogger<ConfigInitializer> _logger;

    public ConfigInitializer(IConfigManager configManager, ILogger<ConfigInitializer> logger)
    {
        _configManager = configManager;
        _logger = logger;
    }

    /// <summary>
    /// Ensures all necessary configuration files exist
    /// </summary>
    public async Task EnsureConfigFilesExistAsync()
    {
        _logger.LogInformation("Initializing configuration files...");

        await _configManager.EnsureFilesExist();
        
        _logger.LogInformation("Configuration files initialized");
    }
}
