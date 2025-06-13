using Common.Exceptions;

namespace Common.Configuration.DownloadClient;

public sealed record DownloadClientConfig : IConfig
{
    /// <summary>
    /// Collection of download clients configured for the application
    /// </summary>
    public List<ClientConfig> Clients { get; init; } = new();
    
    /// <summary>
    /// Gets a client configuration by id
    /// </summary>
    /// <param name="id">The client id</param>
    /// <returns>The client configuration or null if not found</returns>
    public ClientConfig? GetClientConfig(Guid id)
    {
        return Clients.FirstOrDefault(c => c.Id == id);
    }

    /// <summary>
    /// Gets all enabled clients
    /// </summary>
    /// <returns>Collection of enabled client configurations</returns>
    public IEnumerable<ClientConfig> GetEnabledClients()
    {
        return Clients.Where(c => c.Enabled);
    }
    
    /// <summary>
    /// Validates the configuration to ensure it meets requirements
    /// </summary>
    public void Validate()
    {
        // Validate clients have unique IDs
        var duplicateNames = Clients
            .GroupBy(c => c.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
            
        if (duplicateNames.Any())
        {
            throw new ValidationException($"Duplicate client names found: {string.Join(", ", duplicateNames)}");
        }
        
        // Validate each client configuration
        foreach (var client in Clients)
        {
            client.Validate();
        }
    }
}