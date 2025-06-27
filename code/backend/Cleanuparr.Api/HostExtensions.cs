using System.Reflection;
using Cleanuparr.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cleanuparr.Api;

public static class HostExtensions
{
    public static async Task<IHost> Init(this WebApplication app)
    {
        ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();

        Version? version = Assembly.GetExecutingAssembly().GetName().Version;

        logger.LogInformation(
            version is null
                ? "Cleanuparr version not detected"
                : $"Cleanuparr v{version.Major}.{version.Minor}.{version.Build}"
        );
        
        logger.LogInformation("timezone: {tz}", TimeZoneInfo.Local.DisplayName);
        
        // Apply db migrations
        var eventsContext = app.Services.GetRequiredService<EventsContext>();
        if ((await eventsContext.Database.GetPendingMigrationsAsync()).Any())
        {
            await eventsContext.Database.MigrateAsync();
        }

        var configContext = app.Services.GetRequiredService<DataContext>();
        if ((await configContext.Database.GetPendingMigrationsAsync()).Any())
        {
            await configContext.Database.MigrateAsync();
        }
        
        return app;
    }
}