using Microsoft.Extensions.DependencyInjection;
namespace Infrastructure.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services)
    {
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
