namespace Cleanuparr.Infrastructure.Health;

/// <summary>
/// Event arguments for client health changes
/// </summary>
public class ClientHealthChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the client ID
    /// </summary>
    public Guid ClientId { get; }
    
    /// <summary>
    /// Gets the health status
    /// </summary>
    public HealthStatus Status { get; }
    
    /// <summary>
    /// Gets a value indicating whether this is a transition to a healthy state
    /// </summary>
    public bool IsRecovered { get; }
    
    /// <summary>
    /// Gets a value indicating whether this is a transition to an unhealthy state
    /// </summary>
    public bool IsDegraded { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientHealthChangedEventArgs"/> class
    /// </summary>
    /// <param name="clientId">The client ID</param>
    /// <param name="status">The current health status</param>
    /// <param name="previousStatus">The previous health status, if any</param>
    public ClientHealthChangedEventArgs(Guid clientId, HealthStatus status, HealthStatus? previousStatus)
    {
        ClientId = clientId;
        Status = status;
        
        // Determine if this is a state transition
        if (previousStatus != null)
        {
            IsRecovered = !previousStatus.IsHealthy && status.IsHealthy;
            IsDegraded = previousStatus.IsHealthy && !status.IsHealthy;
        }
        else
        {
            IsRecovered = false;
            IsDegraded = !status.IsHealthy;
        }
    }
}
