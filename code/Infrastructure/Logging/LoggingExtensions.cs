using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Infrastructure.Logging;

/// <summary>
/// Extension methods for contextual logging
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Adds a category to log messages
    /// </summary>
    /// <param name="logger">The logger to enrich</param>
    /// <param name="category">The log category</param>
    /// <returns>Enriched logger instance</returns>
    public static ILogger WithCategory(this ILogger logger, string category)
    {
        return logger.BeginScope(new Dictionary<string, object> { ["Category"] = category });
    }
    
    /// <summary>
    /// Adds a job name to log messages
    /// </summary>
    /// <param name="logger">The logger to enrich</param>
    /// <param name="jobName">The job name</param>
    /// <returns>Enriched logger instance</returns>
    public static ILogger WithJob(this ILogger logger, string jobName)
    {
        return logger.BeginScope(new Dictionary<string, object> { ["JobName"] = jobName });
    }
    
    /// <summary>
    /// Adds an instance name to log messages
    /// </summary>
    /// <param name="logger">The logger to enrich</param>
    /// <param name="instanceName">The instance name</param>
    /// <returns>Enriched logger instance</returns>
    public static ILogger WithInstance(this ILogger logger, string instanceName)
    {
        return logger.BeginScope(new Dictionary<string, object> { ["InstanceName"] = instanceName });
    }
    
    /// <summary>
    /// Adds a category to Serilog log messages
    /// </summary>
    /// <param name="logger">The Serilog logger</param>
    /// <param name="category">The log category</param>
    /// <returns>Enriched logger instance</returns>
    public static Serilog.ILogger WithCategory(this Serilog.ILogger logger, string category)
    {
        return logger.ForContext("Category", category);
    }
    
    /// <summary>
    /// Adds a job name to Serilog log messages
    /// </summary>
    /// <param name="logger">The Serilog logger</param>
    /// <param name="jobName">The job name</param>
    /// <returns>Enriched logger instance</returns>
    public static Serilog.ILogger WithJob(this Serilog.ILogger logger, string jobName)
    {
        return logger.ForContext("JobName", jobName);
    }
    
    /// <summary>
    /// Adds an instance name to Serilog log messages
    /// </summary>
    /// <param name="logger">The Serilog logger</param>
    /// <param name="instanceName">The instance name</param>
    /// <returns>Enriched logger instance</returns>
    public static Serilog.ILogger WithInstance(this Serilog.ILogger logger, string instanceName)
    {
        return logger.ForContext("InstanceName", instanceName);
    }
}
