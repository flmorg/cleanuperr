using System.Security.Cryptography;
using System.Text;
using Cleanuparr.Persistence;
using Microsoft.Extensions.Logging;

namespace Cleanuparr.Infrastructure.Features.Security;

/// <summary>
/// Provides AES-128 GCM encryption services for sensitive data using the application's encryption key.
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private readonly ILogger<AesEncryptionService> _logger;
    private readonly byte[] _key;
    private readonly byte[] _nonce;
    private const string EncryptedPrefix = "AES128GCM:";

    public AesEncryptionService(ILogger<AesEncryptionService> logger, DataContext dataContext)
    {
        _logger = logger;
        
        var generalConfig = dataContext.GeneralConfigs.First();
        
        // Derive key and nonce from the GUID string
        var keyBytes = Encoding.UTF8.GetBytes(generalConfig.EncryptionKey);
        
        // Create a 16-byte key for AES-128
        _key = new byte[16];
        Buffer.BlockCopy(keyBytes, 0, _key, 0, Math.Min(keyBytes.Length, 16));
        
        // Use the remaining bytes for the nonce (or use a fixed portion if needed)
        _nonce = new byte[12]; // 12 bytes for GCM nonce
        
        // If the key is longer than 16 bytes, use the additional bytes for the nonce
        if (keyBytes.Length > 16)
        {
            Buffer.BlockCopy(keyBytes, 16, _nonce, 0, Math.Min(keyBytes.Length - 16, 12));
        }
        else
        {
            // Use a different portion of the key for the nonce
            for (int i = 0; i < Math.Min(keyBytes.Length, 12); i++)
            {
                _nonce[i] = keyBytes[keyBytes.Length - i - 1];
            }
        }
        
        _logger.LogDebug("Encryption service initialized");
    }

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }
        
        try
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes;
            byte[] tag = new byte[16]; // GCM authentication tag
            
            using (var aes = new AesGcm(_key))
            {
                cipherBytes = new byte[plainBytes.Length];
                aes.Encrypt(_nonce, plainBytes, cipherBytes, tag);
            }
            
            // Combine nonce, ciphertext, and tag
            byte[] result = new byte[cipherBytes.Length + tag.Length];
            Buffer.BlockCopy(cipherBytes, 0, result, 0, cipherBytes.Length);
            Buffer.BlockCopy(tag, 0, result, cipherBytes.Length, tag.Length);
            
            // Convert to Base64 and add prefix
            return $"{EncryptedPrefix}{Convert.ToBase64String(result)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt value");
            throw;
        }
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText) || !IsEncrypted(cipherText))
        {
            return cipherText;
        }

        try
        {
            // Remove prefix and decode Base64
            string base64 = cipherText.Substring(EncryptedPrefix.Length);
            byte[] encryptedData = Convert.FromBase64String(base64);
            
            // Extract ciphertext and tag
            int cipherLength = encryptedData.Length - 16; // Last 16 bytes are the tag
            byte[] cipherBytes = new byte[cipherLength];
            byte[] tag = new byte[16];
            
            Buffer.BlockCopy(encryptedData, 0, cipherBytes, 0, cipherLength);
            Buffer.BlockCopy(encryptedData, cipherLength, tag, 0, 16);
            
            // Decrypt
            byte[] plainBytes = new byte[cipherLength];
            using (var aes = new AesGcm(_key))
            {
                aes.Decrypt(_nonce, cipherBytes, tag, plainBytes);
            }
            
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt value: {CipherText}", cipherText);
            throw; // As per requirements, we throw an exception on decryption failure
        }
    }

    /// <inheritdoc />
    public bool IsEncrypted(string text)
    {
        return !string.IsNullOrEmpty(text) && text.StartsWith(EncryptedPrefix);
    }
}
