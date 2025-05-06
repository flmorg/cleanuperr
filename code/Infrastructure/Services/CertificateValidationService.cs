using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Common.Configuration.General;
using Common.Enums;
using Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class CertificateValidationService
{
    private readonly ILogger<CertificateValidationService> _logger;
    private readonly HttpConfig _config;

    public CertificateValidationService(ILogger<CertificateValidationService> logger, IOptions<HttpConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    public bool ShouldByPassValidationError(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        var targetHostName = string.Empty;

        if (sender is not SslStream && sender is not string)
        {
            return true;
        }

        if (sender is SslStream request)
        {
            targetHostName = request.TargetHostName;
        }

        // Mailkit passes host in sender as string
        if (sender is string stringHost)
        {
            targetHostName = stringHost;
        }

        if (certificate is X509Certificate2 cert2 && cert2.SignatureAlgorithm.FriendlyName == "md5RSA")
        {
            _logger.LogError(
                $"https://{targetHostName} uses the obsolete md5 hash in its https certificate, if that is your certificate, please (re)create certificate with better algorithm as soon as possible.");
        }

        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        if (targetHostName is "localhost" or "127.0.0.1")
        {
            return true;
        }

        var ipAddresses = GetIpAddresses(targetHostName);

        if (_config.CertificateValidation == CertificateValidationType.Disabled)
        {
            return true;
        }

        if (_config.CertificateValidation == CertificateValidationType.DisabledForLocalAddresses &&
            ipAddresses.All(i => i.IsLocalAddress()))
        {
            return true;
        }

        _logger.LogError($"certificate validation for {targetHostName} failed. {sslPolicyErrors}");

        return false;
    }

    private static IPAddress[] GetIpAddresses(string host)
    {
        if (IPAddress.TryParse(host, out var ipAddress))
        {
            return [ipAddress];
        }

        return Dns.GetHostEntry(host).AddressList;
    }
}