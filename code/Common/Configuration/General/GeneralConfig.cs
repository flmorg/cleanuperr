using Common.Enums;
using Common.Exceptions;
using Newtonsoft.Json;
using Serilog.Events;

namespace Common.Configuration.General;

public sealed record GeneralConfig : IConfig
{
    public bool DryRun { get; init; }
    
    public ushort HttpMaxRetries { get; init; }
    
    public ushort HttpTimeout { get; init; } = 100;
    
    [JsonProperty("http_validate_cert")]
    public CertificateValidationType CertificateValidation { get; init; } = CertificateValidationType.Enabled;

    public bool SearchEnabled { get; init; } = true;
    
    public ushort SearchDelay { get; init; } = 30;
    
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;

    public void Validate()
    {
        if (HttpTimeout is 0)
        {
            throw new ValidationException("HTTP_TIMEOUT must be greater than 0");
        }
    }
}