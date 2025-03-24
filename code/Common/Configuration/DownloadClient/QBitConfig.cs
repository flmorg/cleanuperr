using Common.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Common.Configuration.DownloadClient;

public sealed class QBitConfig : IConfig
{
    public const string SectionName = "qBittorrent";
    
    public Uri? Url { get; init; }
    
    [ConfigurationKeyName("URL_BASE")]
    public string UrlBase { get; init; } = string.Empty;
    
    public string? Username { get; init; }
    
    public string? Password { get; init; }
    
    public void Validate()
    {
        if (Url is null)
        {
            throw new ValidationException($"{nameof(Url)} is empty");
        }
    }
}