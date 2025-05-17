using System.Reflection;
using Infrastructure.Configuration;

namespace Executable;

public static class HostExtensions
{
    public static async Task<IHost> Init(this IHost host)
    {
        ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();

        Version? version = Assembly.GetExecutingAssembly().GetName().Version;

        logger.LogInformation(
            version is null
                ? "cleanuperr version not detected"
                : $"cleanuperr v{version.Major}.{version.Minor}.{version.Build}"
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
        
        return host;
    }
}