namespace Cleanuparr.Infrastructure.Features.Security;

/// <summary>
/// Provides encryption and decryption services for sensitive data.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a plain text string.
    /// </summary>
    /// <param name="plainText">The text to encrypt.</param>
    /// <returns>The encrypted string.</returns>
    string Encrypt(string plainText);
    
    /// <summary>
    /// Decrypts an encrypted string.
    /// </summary>
    /// <param name="cipherText">The encrypted text to decrypt.</param>
    /// <returns>The decrypted plain text string.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Thrown when decryption fails.</exception>
    string Decrypt(string cipherText);
    
    /// <summary>
    /// Checks if a string is in encrypted format.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if the text appears to be encrypted, false otherwise.</returns>
    bool IsEncrypted(string text);
}
