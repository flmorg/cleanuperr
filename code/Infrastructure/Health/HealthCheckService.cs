using Common.Configuration.DownloadClient;
using Infrastructure.Configuration;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.DownloadClient.Factory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Health;

/// <summary>
/// Service for checking the health of download clients
/// </summary>
public class HealthCheckService : IHealthCheckService
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IConfigManager _configManager;
    private readonly IDownloadClientFactory _clientFactory;
    private readonly Dictionary<string, HealthStatus> _healthStatuses = new();
    private readonly object _lockObject = new();

    /// <summary>
    /// Occurs when a client's health status changes
    /// </summary>
    public event EventHandler<ClientHealthChangedEventArgs>? ClientHealthChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthCheckService"/> class
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="configManager">The configuration manager</param>
    /// <param name="clientFactory">The download client factory</param>
    public HealthCheckService(
        ILogger<HealthCheckService> logger,
        IConfigManager configManager,
        IDownloadClientFactory clientFactory)
    {
        _logger = logger;
        _configManager = configManager;
        _clientFactory = clientFactory;
    }

    /// <inheritdoc />
    public async Task<HealthStatus> CheckClientHealthAsync(string clientId)
    {
        _logger.LogDebug("Checking health for client {clientId}", clientId);

        try
        {
            // Get the client configuration
            var config = await GetClientConfigAsync(clientId);
            if (config == null)
            {
                _logger.LogWarning("Client {clientId} not found in configuration", clientId);
                var notFoundStatus = new HealthStatus
                {
                    ClientId = clientId,
                    IsHealthy = false,
                    LastChecked = DateTime.UtcNow,
                    ErrorMessage = "Client not found in configuration"
                };
                
                UpdateHealthStatus(notFoundStatus);
                return notFoundStatus;
            }

            // Get the client instance
            var client = _clientFactory.GetClient(clientId);
            
            // Measure response time
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                // Execute the login to check connectivity
                await client.LoginAsync();
                
                stopwatch.Stop();
                
                // Create health status object
                var status = new HealthStatus
                {
                    ClientId = clientId,
                    ClientName = config.Name,
                    ClientTypeType = config.Type,
                    IsHealthy = true,
                    LastChecked = DateTime.UtcNow,
                    ResponseTime = stopwatch.Elapsed
                };
                
                UpdateHealthStatus(status);
                return status;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _logger.LogWarning(ex, "Health check failed for client {clientId}", clientId);
                
                var status = new HealthStatus
                {
                    ClientId = clientId,
                    ClientName = config.Name,
                    ClientTypeType = config.Type,
                    IsHealthy = false,
                    LastChecked = DateTime.UtcNow,
                    ErrorMessage = $"Connection failed: {ex.Message}",
                    ResponseTime = stopwatch.Elapsed
                };
                
                UpdateHealthStatus(status);
                return status;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health check for client {clientId}", clientId);
            
            var status = new HealthStatus
            {
                ClientId = clientId,
                IsHealthy = false,
                LastChecked = DateTime.UtcNow,
                ErrorMessage = $"Error: {ex.Message}"
            };
            
            UpdateHealthStatus(status);
            return status;
        }
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, HealthStatus>> CheckAllClientsHealthAsync()
    {
        _logger.LogDebug("Checking health for all enabled clients");
        
        try
        {
            // Get all enabled client configurations
            var config = await _configManager.GetDownloadClientConfigAsync();
            if (config == null)
            {
                _logger.LogWarning("Download client configuration not found");
                return new Dictionary<string, HealthStatus>();
            }
            
            var enabledClients = config.GetEnabledClients();
            var results = new Dictionary<string, HealthStatus>();
            
            // Check health of each enabled client
            foreach (var clientConfig in enabledClients)
            {
                var status = await CheckClientHealthAsync(clientConfig.Id);
                results[clientConfig.Id] = status;
            }
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health for all clients");
            return new Dictionary<string, HealthStatus>();
        }
    }

    /// <inheritdoc />
    public HealthStatus? GetClientHealth(string clientId)
    {
        lock (_lockObject)
        {
            return _healthStatuses.TryGetValue(clientId, out var status) ? status : null;
        }
    }

    /// <inheritdoc />
    public IDictionary<string, HealthStatus> GetAllClientHealth()
    {
        lock (_lockObject)
        {
            return new Dictionary<string, HealthStatus>(_healthStatuses);
        }
    }
    
    private async Task<ClientConfig?> GetClientConfigAsync(string clientId)
    {
        var config = await _configManager.GetDownloadClientConfigAsync();
        return config?.GetClientConfig(clientId);
    }
    
    private void UpdateHealthStatus(HealthStatus newStatus)
    {
        HealthStatus? previousStatus = null;
        
        lock (_lockObject)
        {
            // Get previous status for comparison
            _healthStatuses.TryGetValue(newStatus.ClientId, out previousStatus);
            
            // Update status
            _healthStatuses[newStatus.ClientId] = newStatus;
        }
        
        // Determine if there's a significant change
        bool isStateChange = previousStatus == null || 
                             previousStatus.IsHealthy != newStatus.IsHealthy;

        // Raise event if there's a significant change
        if (isStateChange)
        {
            _logger.LogInformation(
                "Client {clientId} health changed: {status}", 
                newStatus.ClientId, 
                newStatus.IsHealthy ? "Healthy" : "Unhealthy");
            
            OnClientHealthChanged(new ClientHealthChangedEventArgs(
                newStatus.ClientId, 
                newStatus, 
                previousStatus));
        }
    }
    
    private void OnClientHealthChanged(ClientHealthChangedEventArgs e)
    {
        ClientHealthChanged?.Invoke(this, e);
    }
}
