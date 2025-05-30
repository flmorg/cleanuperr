using Common.Enums;

namespace Common.Configuration.DTOs.DownloadClient;

/// <summary>
/// DTO for retrieving DownloadClient configuration (excludes sensitive data)
/// </summary>
public class DownloadClientConfigDto
{
    /// <summary>
    /// Collection of download clients configured for the application
    /// </summary>
    public List<ClientConfigDto> Clients { get; set; } = new();
}

/// <summary>
/// DTO for individual client configuration (excludes sensitive data)
/// </summary>
public class ClientConfigDto
{
    /// <summary>
    /// Whether this client is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Unique identifier for this client
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Friendly name for this client
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of download client
    /// </summary>
    public DownloadClientType Type { get; set; } = DownloadClientType.None;
    
    /// <summary>
    /// Host address for the download client
    /// </summary>
    public string Host { get; set; } = string.Empty;
    
    /// <summary>
    /// Username for authentication (included without password)
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// The base URL path component, used by clients like Transmission and Deluge
    /// </summary>
    public string UrlBase { get; set; } = string.Empty;
}
