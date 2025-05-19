namespace Infrastructure.Logging;

/// <summary>
/// Standard logging categories used throughout the application
/// </summary>
public static class LoggingCategoryConstants
{
    /// <summary>
    /// System-level logs (startup, configuration, etc.)
    /// </summary>
    public const string System = "SYSTEM";
    
    /// <summary>
    /// API-related logs (requests, responses, etc.)
    /// </summary>
    public const string Api = "API";
    
    /// <summary>
    /// Job-related logs (scheduled tasks, background operations)
    /// </summary>
    public const string Jobs = "JOBS";
    
    /// <summary>
    /// Notification-related logs (user alerts, warnings)
    /// </summary>
    public const string Notifications = "NOTIFICATIONS";
    
    /// <summary>
    /// Sonarr-related logs
    /// </summary>
    public const string Sonarr = "SONARR";
    
    /// <summary>
    /// Radarr-related logs
    /// </summary>
    public const string Radarr = "RADARR";
    
    /// <summary>
    /// Lidarr-related logs
    /// </summary>
    public const string Lidarr = "LIDARR";
}
