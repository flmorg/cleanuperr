using System.Reflection;

namespace Executable;

public static class HostExtensions
{
    public static IHost Init(this IHost host)
    {
        ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();

        Version? version = Assembly.GetExecutingAssembly().GetName().Version;

        logger.LogInformation(
            version is null
                ? "cleanuperr version not detected"
                : $"cleanuperr v{version.Major}.{version.Minor}.{version.Build}"
        );
        
        logger.LogInformation("timezone: {tz}", TimeZoneInfo.Local.DisplayName);
        
        return host;
    }
}