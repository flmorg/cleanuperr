using Infrastructure.Configuration;
using System.IO;

namespace Executable.DependencyInjection;

public static class ConfigurationDI
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // We no longer configure options from appsettings.json
        // Instead, we rely solely on JSON configuration files
        
        // Define the configuration directory
        string configDirectory = Path.Combine(
            Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? AppDomain.CurrentDomain.BaseDirectory,
            "config");
            
        // Ensure the configuration directory exists
        Directory.CreateDirectory(configDirectory);
        
        // Add JSON-based configuration services
        services.AddConfigurationServices(configDirectory);
        
        return services;
    }
}