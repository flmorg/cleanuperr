namespace Cleanuparr.Api.Models;

/// <summary>
/// Standardized error response model for API endpoints
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// User-friendly error message
    /// </summary>
    public required string Error { get; set; }
    
    /// <summary>
    /// Trace ID for error tracking (GUID)
    /// </summary>
    public required string TraceId { get; set; }
}
