using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services, string configDirectory)
    {
        services.AddSingleton<JsonConfigurationProvider>(provider => 
        {
            var logger = provider.GetRequiredService<ILogger<JsonConfigurationProvider>>();
            return new JsonConfigurationProvider(logger, configDirectory);
        });
        
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        
        return services;
    }
}
