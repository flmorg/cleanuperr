using Common.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Common.Configuration.DownloadClient;

public sealed record DelugeConfig : IConfig
{
    public const string SectionName = "Deluge";
    
    public Uri? Url { get; init; }
    
    [ConfigurationKeyName("URL_BASE")]
    public string UrlBase { get; init; } = string.Empty;
    
    public string? Password { get; init; }
    
    public void Validate()
    {
        if (Url is null)
        {
            throw new ValidationException($"{nameof(Url)} is empty");
        }
    }
}