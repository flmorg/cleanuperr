using Microsoft.Extensions.Configuration;

namespace Common.Configuration.DownloadClient;

/// <summary>
/// Configuration for a specific download client
/// </summary>
public sealed record ClientConfig
{
    /// <summary>
    /// Unique identifier for this client
    /// </summary>
    [ConfigurationKeyName("ID")]
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    
    /// <summary>
    /// Friendly name for this client
    /// </summary>
    [ConfigurationKeyName("NAME")]
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of download client
    /// </summary>
    [ConfigurationKeyName("TYPE")]
    public Common.Enums.DownloadClient Type { get; init; } = Common.Enums.DownloadClient.None;
    
    /// <summary>
    /// Host address for the download client
    /// </summary>
    [ConfigurationKeyName("HOST")]
    public string Host { get; init; } = string.Empty;
    
    /// <summary>
    /// Port for the download client
    /// </summary>
    [ConfigurationKeyName("PORT")]
    public int Port { get; init; }
    
    /// <summary>
    /// Username for authentication
    /// </summary>
    [ConfigurationKeyName("USERNAME")]
    public string Username { get; init; } = string.Empty;
    
    /// <summary>
    /// Password for authentication
    /// </summary>
    [ConfigurationKeyName("PASSWORD")]
    public string Password { get; init; } = string.Empty;
    
    /// <summary>
    /// Default category to use
    /// </summary>
    [ConfigurationKeyName("CATEGORY")]
    public string Category { get; init; } = string.Empty;
    
    /// <summary>
    /// Path to download directory
    /// </summary>
    [ConfigurationKeyName("PATH")]
    public string Path { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether this client is enabled
    /// </summary>
    [ConfigurationKeyName("ENABLED")]
    public bool Enabled { get; init; } = true;
}
