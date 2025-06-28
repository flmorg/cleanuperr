using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Cleanuparr.Infrastructure.Health;

/// <summary>
/// Health check that verifies download clients are healthy
/// </summary>
public class DownloadClientsHealthCheck : IHealthCheck
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly ILogger<DownloadClientsHealthCheck> _logger;

    public DownloadClientsHealthCheck(IHealthCheckService healthCheckService, ILogger<DownloadClientsHealthCheck> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current health status of all clients without triggering new checks
            var allClientHealth = _healthCheckService.GetAllClientHealth();
            
            if (!allClientHealth.Any())
            {
                // No clients configured - this might be ok depending on the deployment
                return HealthCheckResult.Healthy("No download clients configured");
            }

            var healthyClients = allClientHealth.Values.Where(h => h.IsHealthy).ToList();
            var unhealthyClients = allClientHealth.Values.Where(h => !h.IsHealthy).ToList();
            var totalClients = allClientHealth.Count;

            if (unhealthyClients.Any())
            {
                var unhealthyNames = string.Join(", ", unhealthyClients.Select(c => c.ClientName));
                var message = $"{unhealthyClients.Count}/{totalClients} download clients unhealthy: {unhealthyNames}";
                
                // If more than half are unhealthy, consider it unhealthy
                if (unhealthyClients.Count > totalClients / 2)
                {
                    return HealthCheckResult.Unhealthy(message);
                }
                
                // Otherwise, it's degraded
                return HealthCheckResult.Degraded(message);
            }

            return HealthCheckResult.Healthy($"All {totalClients} download clients are healthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download clients health check failed");
            return HealthCheckResult.Unhealthy("Download clients health check failed", ex);
        }
    }
} 