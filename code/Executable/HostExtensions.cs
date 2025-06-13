using System.Reflection;
using Data;
using Infrastructure.Configuration;
using Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

namespace Executable;

public static class HostExtensions
{
    public static async Task<IHost> Init(this IHost host)
    {
        ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();

        Version? version = Assembly.GetExecutingAssembly().GetName().Version;

        logger.LogInformation(
            version is null
                ? "Cleanuparr version not detected"
                : $"Cleanuparr v{version.Major}.{version.Minor}.{version.Build}"
        );
        
        logger.LogInformation("timezone: {tz}", TimeZoneInfo.Local.DisplayName);
        
        // Initialize configuration files
        try
        {
            var configInitializer = host.Services.GetRequiredService<ConfigInitializer>();
            await configInitializer.EnsureConfigFilesExistAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize configuration files");
        }
        
        // Apply db migrations
        var eventsContext = host.Services.GetRequiredService<EventsContext>();
        if ((await eventsContext.Database.GetPendingMigrationsAsync()).Any())
        {
            await eventsContext.Database.MigrateAsync();
        }

        var configContext = host.Services.GetRequiredService<DataContext>();
        if ((await configContext.Database.GetPendingMigrationsAsync()).Any())
        {
            await configContext.Database.MigrateAsync();
        }
        
        return host;
    }
}