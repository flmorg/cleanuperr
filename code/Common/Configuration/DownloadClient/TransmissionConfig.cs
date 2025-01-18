using Microsoft.Extensions.Configuration;

namespace Common.Configuration.DownloadClient;

public record TransmissionConfig : IConfig
{
    public const string SectionName = "Transmission";
    
    public Uri? Url { get; init; }
    
    [ConfigurationKeyName("URL_BASE")]
    public string UrlBase { get; init; } = "transmission";
    
    public string? Username { get; init; }
    
    public string? Password { get; init; }
    
    public void Validate()
    {
        if (Url is null)
        {
            throw new ArgumentNullException(nameof(Url));
        }
    }
}