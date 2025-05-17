using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services, string configDirectory)
    {
        // Create settings subdirectory path
        var settingsDirectory = Path.Combine(configDirectory, "settings");

        // Register the base JSON provider
        services.AddSingleton<JsonConfigurationProvider>(provider => 
        {
            var logger = provider.GetRequiredService<ILogger<JsonConfigurationProvider>>();
            return new JsonConfigurationProvider(logger, configDirectory);
        });
        
        // Register the cached provider as the implementation of IConfigurationProvider
        services.AddSingleton<IConfigurationProvider, CachedConfigurationProvider>(provider => 
        {
            var logger = provider.GetRequiredService<ILogger<CachedConfigurationProvider>>();
            var baseProvider = provider.GetRequiredService<JsonConfigurationProvider>();
            return new CachedConfigurationProvider(logger, baseProvider, settingsDirectory);
        });
        
        // Register config manager and initializer
        services.AddSingleton<IConfigManager, ConfigManager>();
        services.AddSingleton<ConfigInitializer>();
        
        return services;
    }
}
