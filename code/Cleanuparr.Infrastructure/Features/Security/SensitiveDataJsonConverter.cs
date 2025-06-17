using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Cleanuparr.Infrastructure.Features.Security;

/// <summary>
/// Handles encryption and decryption of sensitive data during JSON serialization and deserialization.
/// </summary>
public class SensitiveDataJsonConverter : JsonConverter<string>
{
    private readonly ILogger<SensitiveDataJsonConverter> _logger;
    private readonly IEncryptionService _encryptionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SensitiveDataJsonConverter"/> class.
    /// </summary>
    /// <param name="encryptionService">The encryption service to use.</param>
    /// <param name="logger">The logger instance.</param>
    public SensitiveDataJsonConverter(ILogger<SensitiveDataJsonConverter> logger, IEncryptionService encryptionService)
    {
        _logger = logger;
        _encryptionService = encryptionService;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        // Only apply to string properties marked with the SensitiveData attribute
        return typeToConvert == typeof(string);
    }

    /// <inheritdoc />
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        try
        {
            // If the value is encrypted, decrypt it
            if (_encryptionService.IsEncrypted(value))
            {
                return _encryptionService.Decrypt(value);
            }
            
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt sensitive value during JSON deserialization");
            throw; // As per requirements, we throw an exception on decryption failure
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        if (string.IsNullOrEmpty(value))
        {
            writer.WriteStringValue(value);
            return;
        }

        try
        {
            // Don't re-encrypt already encrypted values
            if (_encryptionService.IsEncrypted(value))
            {
                writer.WriteStringValue(value);
                return;
            }
            
            // Encrypt the value
            string encrypted = _encryptionService.Encrypt(value);
            writer.WriteStringValue(encrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt sensitive value during JSON serialization");
            throw;
        }
    }
}
