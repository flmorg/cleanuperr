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
    public ClientConfig? GetClientConfig(string id)
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
        var duplicateIds = Clients
            .GroupBy(c => c.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
            
        if (duplicateIds.Any())
        {
            throw new InvalidOperationException($"Duplicate client IDs found: {string.Join(", ", duplicateIds)}");
        }
        
        // Validate each client configuration
        foreach (var client in Clients)
        {
            if (string.IsNullOrWhiteSpace(client.Id))
            {
                throw new InvalidOperationException("Client ID cannot be empty");
            }
            
            if (string.IsNullOrWhiteSpace(client.Name))
            {
                throw new InvalidOperationException($"Client name cannot be empty for client ID: {client.Id}");
            }
            
            if (string.IsNullOrWhiteSpace(client.Host))
            {
                throw new InvalidOperationException($"Host cannot be empty for client ID: {client.Id}");
            }
        }
    }
}