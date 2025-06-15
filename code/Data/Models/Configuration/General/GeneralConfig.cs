using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Common.Configuration;
using Common.Enums;
using Serilog.Events;
using ValidationException = Common.Exceptions.ValidationException;

namespace Data.Models.Configuration.General;

public sealed record GeneralConfig : IConfig
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public bool DryRun { get; set; }
    
    public ushort HttpMaxRetries { get; set; }
    
    public ushort HttpTimeout { get; set; } = 100;
    
    public CertificateValidationType HttpCertificateValidation { get; set; } = CertificateValidationType.Enabled;

    public bool SearchEnabled { get; set; } = true;
    
    public ushort SearchDelay { get; set; } = 30;
    
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;

    public string EncryptionKey { get; set; } = Guid.NewGuid().ToString();

    public List<string> IgnoredDownloads { get; set; } = [];

    public void Validate()
    {
        if (HttpTimeout is 0)
        {
            throw new ValidationException("HTTP_TIMEOUT must be greater than 0");
        }
    }
}