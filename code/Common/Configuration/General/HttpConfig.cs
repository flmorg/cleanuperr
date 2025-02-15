using Common.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Common.Configuration.General;

public sealed record HttpConfig : IConfig
{
    [ConfigurationKeyName("HTTP_MAX_RETRIES")]
    public ushort MaxRetries { get; init; }
    
    [ConfigurationKeyName("HTTP_TIMEOUT")]
    public ushort Timeout { get; init; } = 100;

    public void Validate()
    {
        if (Timeout is 0)
        {
            throw new ValidationException("HTTP_TIMEOUT must be greater than 0");
        }
    }
}