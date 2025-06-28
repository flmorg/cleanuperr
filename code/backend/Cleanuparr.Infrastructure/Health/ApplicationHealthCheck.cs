
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cleanuparr.Infrastructure.Health;

/// <summary>
/// Basic application health check that verifies the application is running
/// </summary>
public class ApplicationHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Basic liveness check - if we can execute this, the app is running
        return Task.FromResult(HealthCheckResult.Healthy("Application is running"));
    }
} 