using Cleanuparr.Domain.Enums;
using Cleanuparr.Domain.Exceptions;

namespace Cleanuparr.Application.Features.DownloadClient.Dtos;

/// <summary>
/// DTO for creating a new download client (without ID)
/// </summary>
public sealed record CreateDownloadClientDto
{
    /// <summary>
    /// Whether this client is enabled
    /// </summary>
    public bool Enabled { get; init; } = false;
    
    /// <summary>
    /// Friendly name for this client
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Type name of download client
    /// </summary>
    public required DownloadClientTypeName TypeName { get; init; }
    
    /// <summary>
    /// Type of download client
    /// </summary>
    public required DownloadClientType Type { get; init; }
    
    /// <summary>
    /// Host address for the download client
    /// </summary>
    public Uri? Host { get; init; }
    
    /// <summary>
    /// Username for authentication
    /// </summary>
    public string? Username { get; init; }
    
    /// <summary>
    /// Password for authentication
    /// </summary>
    public string? Password { get; init; }
    
    /// <summary>
    /// The base URL path component, used by clients like Transmission and Deluge
    /// </summary>
    public string? UrlBase { get; init; }
    
    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ValidationException("Client name cannot be empty");
        }
        
        if (Host is null)
        {
            throw new ValidationException("Host cannot be empty");
        }
    }
}