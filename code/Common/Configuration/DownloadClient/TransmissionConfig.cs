using Common.Exceptions;

namespace Common.Configuration.DownloadClient;

public record TransmissionConfig : IConfig
{
    public const string SectionName = "Transmission";
    
    public Uri? Url { get; init; }
    
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