using Common.Enums;
using Common.Exceptions;
using Newtonsoft.Json;

namespace Common.Configuration.General;

public sealed record HttpConfig : IConfig
{
    [JsonProperty("http_max_retries")]
    public ushort MaxRetries { get; init; }
    
    [JsonProperty("http_timeout")]
    public ushort Timeout { get; init; } = 100;
    
    [JsonProperty("http_validate_cert")]
    public CertificateValidationType CertificateValidation { get; init; } = CertificateValidationType.Enabled;

    public void Validate()
    {
        if (Timeout is 0)
        {
            throw new ValidationException("HTTP_TIMEOUT must be greater than 0");
        }
    }
}