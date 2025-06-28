using Cleanuparr.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Cleanuparr.Infrastructure.Health;

/// <summary>
/// Health check that verifies database connectivity
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly DataContext _dataContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(DataContext dataContext, ILogger<DatabaseHealthCheck> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to execute a simple query to verify database connectivity
            var canConnect = await _dataContext.Database.CanConnectAsync(cancellationToken);
            
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            // Optionally check if database schema is up to date
            var pendingMigrations = await _dataContext.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pendingMigrations.Any())
            {
                _logger.LogWarning("Database has pending migrations: {migrations}", string.Join(", ", pendingMigrations));
                return HealthCheckResult.Degraded($"Database has {pendingMigrations.Count()} pending migrations");
            }

            return HealthCheckResult.Healthy("Database connection successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database health check failed", ex);
        }
    }
} 