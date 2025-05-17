using Infrastructure.Configuration;
using System.IO;

namespace Executable.DependencyInjection;

public static class ConfigurationDI
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // We no longer configure options from appsettings.json
        // Instead, we rely solely on JSON configuration files
        
        // Add JSON-based configuration services with Docker-aware path detection
        // and automatic caching with real-time change detection
        services.AddConfigurationServices();
        
        return services;
    }
}