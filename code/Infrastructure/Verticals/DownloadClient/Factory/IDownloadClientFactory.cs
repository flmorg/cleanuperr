using Common.Enums;

namespace Infrastructure.Verticals.DownloadClient.Factory;

/// <summary>
/// Factory for creating and managing download client service instances
/// </summary>
public interface IDownloadClientFactory
{
    /// <summary>
    /// Gets a download client by its ID
    /// </summary>
    /// <param name="clientId">The client ID</param>
    /// <returns>The download service for the specified client</returns>
    IDownloadService GetClient(Guid clientId);
    
    /// <summary>
    /// Gets all enabled download clients
    /// </summary>
    /// <returns>Collection of enabled download client services</returns>
    IEnumerable<IDownloadService> GetAllEnabledClients();
    
    /// <summary>
    /// Gets all enabled download clients of a specific type
    /// </summary>
    /// <param name="clientType">The client type</param>
    /// <returns>Collection of enabled download client services of the specified type</returns>
    IEnumerable<IDownloadService> GetClientsByType(DownloadClientType clientType);
    
    /// <summary>
    /// Refreshes a specific client instance (disposes and recreates)
    /// </summary>
    /// <param name="clientId">The client ID to refresh</param>
    void RefreshClient(Guid clientId);
    
    /// <summary>
    /// Refreshes all client instances (disposes and recreates)
    /// </summary>
    void RefreshAllClients();
}
