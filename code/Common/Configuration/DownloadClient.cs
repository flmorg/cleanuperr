using System.ComponentModel.DataAnnotations.Schema;
using Common.Attributes;
using Common.Enums;
using Common.Exceptions;
using Newtonsoft.Json;

namespace Common.Configuration;

/// <summary>
/// Configuration for a specific download client
/// </summary>
public sealed record DownloadClient
{
    /// <summary>
    /// Unique identifier for this client
    /// </summary>
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Whether this client is enabled
    /// </summary>
    public bool Enabled { get; init; } = false;
    
    /// <summary>
    /// Friendly name for this client
    /// </summary>
    public required string Name { get; init; }
    
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
    [SensitiveData]
    public string? Username { get; init; }
    
    /// <summary>
    /// Password for authentication
    /// </summary>
    [SensitiveData]
    public string? Password { get; init; }
    
    /// <summary>
    /// The base URL path component, used by clients like Transmission and Deluge
    /// </summary>
    [JsonProperty("url_base")]
    public string? UrlBase { get; init; }
    
    /// <summary>
    /// The computed full URL for the client
    /// </summary>
    public Uri Url => new($"{Host?.ToString().TrimEnd('/')}/{UrlBase.TrimStart('/').TrimEnd('/')}");
    
    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ValidationException($"Client name cannot be empty for client ID: {Id}");
        }
        
        if (Host is null && Type is not DownloadClientType.Usenet)
        {
            throw new ValidationException($"Host cannot be empty for client ID: {Id}");
        }
    }
}
