using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;

namespace Cleanuparr.Api;

/// <summary>
/// Custom health check response writers for different formats
/// </summary>
public static class HealthCheckResponseWriter
{
    /// <summary>
    /// Writes a minimal plain text response suitable for Docker health checks
    /// </summary>
    public static async Task WriteMinimalPlaintext(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "text/plain";
        
        var status = report.Status switch
        {
            HealthStatus.Healthy => "healthy",
            HealthStatus.Degraded => "degraded", 
            HealthStatus.Unhealthy => "unhealthy",
            _ => "unknown"
        };

        await context.Response.WriteAsync(status, Encoding.UTF8);
    }
} 