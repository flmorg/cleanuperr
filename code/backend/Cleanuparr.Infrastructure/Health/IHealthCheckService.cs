namespace Cleanuparr.Infrastructure.Health;

/// <summary>
/// Service for checking the health of download clients
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Occurs when a client's health status changes
    /// </summary>
    event EventHandler<ClientHealthChangedEventArgs> ClientHealthChanged;
    
    /// <summary>
    /// Checks the health of a specific client
    /// </summary>
    /// <param name="clientId">The client ID to check</param>
    /// <returns>The health status of the client</returns>
    Task<HealthStatus> CheckClientHealthAsync(Guid clientId);
    
    /// <summary>
    /// Checks the health of all enabled clients
    /// </summary>
    /// <returns>A dictionary of client IDs to health statuses</returns>
    Task<IDictionary<Guid, HealthStatus>> CheckAllClientsHealthAsync();
    
    /// <summary>
    /// Gets the current health status of a client
    /// </summary>
    /// <param name="clientId">The client ID</param>
    /// <returns>The current health status, or null if the client hasn't been checked</returns>
    HealthStatus? GetClientHealth(Guid clientId);
    
    /// <summary>
    /// Gets the current health status of all clients that have been checked
    /// </summary>
    /// <returns>A dictionary of client IDs to health statuses</returns>
    IDictionary<Guid, HealthStatus> GetAllClientHealth();
}
