namespace Common.Configuration.DownloadClient;

/// <summary>
/// Configuration for a specific download client
/// </summary>
public sealed record ClientConfig
{
    /// <summary>
    /// Unique identifier for this client
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    
    /// <summary>
    /// Friendly name for this client
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of download client
    /// </summary>
    public Common.Enums.DownloadClient Type { get; init; } = Common.Enums.DownloadClient.None;
    
    /// <summary>
    /// Host address for the download client
    /// </summary>
    public string Host { get; init; } = string.Empty;
    
    /// <summary>
    /// Port for the download client
    /// </summary>
    public int Port { get; init; }
    
    /// <summary>
    /// Username for authentication
    /// </summary>
    public string Username { get; init; } = string.Empty;
    
    /// <summary>
    /// Password for authentication
    /// </summary>
    public string Password { get; init; } = string.Empty;
    
    /// <summary>
    /// Default category to use
    /// </summary>
    public string Category { get; init; } = string.Empty;
    
    /// <summary>
    /// Path to download directory
    /// </summary>
    public string Path { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether this client is enabled
    /// </summary>
    public bool Enabled { get; init; } = true;
}
