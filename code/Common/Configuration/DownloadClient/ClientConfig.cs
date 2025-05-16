using Common.Enums;
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
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    
    /// <summary>
    /// Friendly name for this client
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of download client
    /// </summary>
    public DownloadClientType Type { get; init; } = DownloadClientType.None;
    
    /// <summary>
    /// Host address for the download client
    /// </summary>
    public string Host { get; init; } = string.Empty;
    
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
    
    /// <summary>
    /// The base URL path component, used by clients like Transmission and Deluge
    /// </summary>
    [ConfigurationKeyName("URL_BASE")]
    public string UrlBase { get; init; } = string.Empty;
    
    /// <summary>
    /// Use HTTPS protocol
    /// </summary>
    public bool UseHttps { get; init; } = false;
    
    /// <summary>
    /// The computed full URL for the client
    /// </summary>
    public Uri Url => new($"{Host.TrimEnd('/')}/{UrlBase.TrimStart('/').TrimEnd('/')}");
    
    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException("Client ID cannot be empty");
        }
        
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException($"Client name cannot be empty for client ID: {Id}");
        }
        
        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new InvalidOperationException($"Host cannot be empty for client ID: {Id}");
        }
        
        if (Type == DownloadClientType.None)
        {
            throw new InvalidOperationException($"Client type must be specified for client ID: {Id}");
        }
    }
}
