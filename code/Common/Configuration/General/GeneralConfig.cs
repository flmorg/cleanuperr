using Common.Enums;
using Common.Exceptions;
using Serilog.Events;

namespace Common.Configuration.General;

public sealed record GeneralConfig : IConfig
{
    public bool DryRun { get; init; }
    
    public ushort HttpMaxRetries { get; init; }
    
    public ushort HttpTimeout { get; init; } = 100;
    
    public CertificateValidationType HttpCertificateValidation { get; init; } = CertificateValidationType.Enabled;

    public bool SearchEnabled { get; init; } = true;
    
    public ushort SearchDelay { get; init; } = 30;
    
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;

    public string EncryptionKey { get; init; } = Guid.NewGuid().ToString();

    public List<string> IgnoredDownloads { get; set; } = [];

    public void Validate()
    {
        if (HttpTimeout is 0)
        {
            throw new ValidationException("HTTP_TIMEOUT must be greater than 0");
        }
    }
}