namespace Common.Configuration.DTOs.DownloadClient;

/// <summary>
/// DTO for updating DownloadClient configuration (includes sensitive data fields)
/// </summary>
public class DownloadClientConfigUpdateDto : DownloadClientConfigDto
{
    /// <summary>
    /// Collection of clients for updating (with sensitive data fields)
    /// </summary>
    public new List<ClientConfigUpdateDto> Clients { get; set; } = new();
}

/// <summary>
/// DTO for updating individual client configuration (includes sensitive data fields)
/// </summary>
public class ClientConfigUpdateDto : ClientConfigDto
{
    /// <summary>
    /// Password for authentication (only included in update DTO)
    /// </summary>
    public string? Password { get; set; }
}
