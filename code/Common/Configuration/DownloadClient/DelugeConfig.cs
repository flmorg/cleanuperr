using Common.Exceptions;

namespace Common.Configuration.DownloadClient;

public sealed record DelugeConfig : IConfig
{
    public const string SectionName = "Deluge";
    
    public Uri? Url { get; init; }
    
    public string? Password { get; init; }
    
    public void Validate()
    {
        if (Url is null)
        {
            throw new ValidationException($"{nameof(Url)} is empty");
        }
    }
}