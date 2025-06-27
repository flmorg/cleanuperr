namespace Cleanuparr.Infrastructure.Http.DynamicHttpClientSystem;

/// <summary>
/// Service for invalidating cached HTTP client configurations
/// </summary>
public interface IHttpClientOptionsInvalidator
{
    /// <summary>
    /// Invalidates the cached configuration for a specific client name
    /// </summary>
    /// <param name="clientName">The name of the client to invalidate</param>
    void InvalidateClient(string clientName);
    
    /// <summary>
    /// Invalidates all cached HTTP client configurations
    /// </summary>
    void InvalidateAllClients();
    
    /// <summary>
    /// Invalidates multiple client configurations
    /// </summary>
    /// <param name="clientNames">The names of the clients to invalidate</param>
    void InvalidateClients(IEnumerable<string> clientNames);
} 