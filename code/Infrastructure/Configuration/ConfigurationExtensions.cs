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
        services.AddSingleton<JsonConfigurationProvider>(provider => 
        {
            var logger = provider.GetRequiredService<ILogger<JsonConfigurationProvider>>();
            var pathProvider = provider.GetRequiredService<ConfigurationPathProvider>();
            return new JsonConfigurationProvider(logger, pathProvider.GetConfigPath());
        });
        
        // Register the cached provider as the implementation of IConfigurationProvider
        services.AddSingleton<IConfigurationProvider, CachedConfigurationProvider>(provider => 
        {
            var logger = provider.GetRequiredService<ILogger<CachedConfigurationProvider>>();
            var baseProvider = provider.GetRequiredService<JsonConfigurationProvider>();
            var pathProvider = provider.GetRequiredService<ConfigurationPathProvider>();
            return new CachedConfigurationProvider(logger, baseProvider, pathProvider.GetSettingsPath());
        });
        
        // Register config manager and initializer
        services.AddSingleton<IConfigManager, ConfigManager>();
        services.AddSingleton<ConfigInitializer>();
        
        // Register the configuration preloader as a hosted service
        services.AddHostedService<ConfigurationPreloader>();
        
        return services;
    }
}
