using Common.Enums;
using Serilog.Events;

namespace Common.Configuration.DTOs.General;

/// <summary>
/// DTO for retrieving General configuration (excludes sensitive data)
/// </summary>
public class GeneralConfigDto
{
    /// <summary>
    /// Whether the application is running in dry run mode
    /// </summary>
    public bool DryRun { get; set; }
    
    /// <summary>
    /// Maximum number of HTTP retries
    /// </summary>
    public ushort HttpMaxRetries { get; set; }
    
    /// <summary>
    /// HTTP timeout in seconds
    /// </summary>
    public ushort HttpTimeout { get; set; } = 100;
    
    /// <summary>
    /// Certificate validation type for HTTP requests
    /// </summary>
    public CertificateValidationType HttpCertificateValidation { get; set; } = CertificateValidationType.Enabled;

    /// <summary>
    /// Whether search functionality is enabled
    /// </summary>
    public bool SearchEnabled { get; set; } = true;
    
    /// <summary>
    /// Delay between searches in seconds
    /// </summary>
    public ushort SearchDelay { get; set; } = 30;
    
    /// <summary>
    /// Application log level
    /// </summary>
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;

    /// <summary>
    /// Ignored downloads list
    /// </summary>
    public List<string> IgnoredDownloads { get; set; } = [];
}
