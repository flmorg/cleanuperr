using Cleanuparr.Shared.Helpers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Cleanuparr.Infrastructure.Health;

/// <summary>
/// Health check that verifies file system access to critical directories
/// </summary>
public class FileSystemHealthCheck : IHealthCheck
{
    private readonly ILogger<FileSystemHealthCheck> _logger;

    public FileSystemHealthCheck(ILogger<FileSystemHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var issues = new List<string>();

            // Check config directory access
            var configPath = ConfigurationPathProvider.GetConfigPath();
            if (!CheckDirectoryAccess(configPath, "config"))
            {
                issues.Add($"Cannot access config directory: {configPath}");
            }

            // Check current working directory
            var currentDir = Directory.GetCurrentDirectory();
            if (!CheckDirectoryAccess(currentDir, "working"))
            {
                issues.Add($"Cannot access working directory: {currentDir}");
            }

            if (issues.Any())
            {
                var message = $"File system issues detected: {string.Join(", ", issues)}";
                return Task.FromResult(HealthCheckResult.Unhealthy(message));
            }

            return Task.FromResult(HealthCheckResult.Healthy("File system access verified"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File system health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("File system health check failed", ex));
        }
    }

    private bool CheckDirectoryAccess(string path, string description)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                _logger.LogWarning("Directory does not exist: {path} ({description})", path, description);
                return false;
            }

            // Try to enumerate directory contents
            _ = Directory.GetFiles(path).Take(1).ToList();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot access {description} directory: {path}", description, path);
            return false;
        }
    }


} 