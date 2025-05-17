using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services)
    {
        // Register path provider to handle Docker vs local environment
        services.AddSingleton<ConfigurationPathProvider>();
        
        // Register the base JSON provider
        services.AddSingleton<JsonConfigurationProvider>();
        
        // Register the cached provider as the implementation of IConfigurationProvider
        services.AddSingleton<IConfigurationProvider, CachedConfigurationProvider>();
        
        // Register config manager and initializer
        services.AddSingleton<IConfigManager, ConfigManager>();
        services.AddSingleton<ConfigInitializer>();
        
        return services;
    }
}
