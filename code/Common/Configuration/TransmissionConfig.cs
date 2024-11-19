namespace Common.Configuration;

public record TransmissionConfig
{
    public const string SectionName = "Transmission";
    
    public required bool Enabled { get; init; }
    
    public Uri? Url { get; init; }
    
    public string? Username { get; init; }
    
    public string? Password { get; init; }
    
    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (Url is null)
        {
            throw new ArgumentNullException(nameof(Url));
        }
    }
}