using System.Text.RegularExpressions;

namespace Cleanuparr.Shared.Helpers;

/// <summary>
/// Validates BASE_PATH values to ensure security and proper formatting
/// </summary>
public static class BasePathValidator
{
    private const int MaxLength = 100;
    private static readonly Regex ValidPathRegex = new(@"^/[a-zA-Z0-9_\-]+(/[a-zA-Z0-9_\-]+)*$", RegexOptions.Compiled);

    /// <summary>
    /// Validates a BASE_PATH value
    /// </summary>
    /// <param name="basePath">The base path to validate</param>
    /// <returns>ValidationResult containing success status and error message if invalid</returns>
    public static ValidationResult Validate(string? basePath)
    {
        // Empty or null is valid (no base path)
        if (string.IsNullOrWhiteSpace(basePath))
        {
            return ValidationResult.Success();
        }

        // Trim whitespace
        basePath = basePath.Trim();
        
        // Check for just root path
        if (basePath == "/")
        {
            return ValidationResult.Failure("BASE_PATH cannot be just '/' (conflicts with root)");
        }

        // Check length
        if (basePath.Length > MaxLength)
        {
            return ValidationResult.Failure($"BASE_PATH cannot be longer than {MaxLength} characters");
        }

        // Must start with /
        if (!basePath.StartsWith('/'))
        {
            return ValidationResult.Failure("BASE_PATH must start with '/'");
        }

        // No dots allowed (prevents path traversal)
        if (basePath.Contains('.'))
        {
            return ValidationResult.Failure("BASE_PATH cannot contain dots (.) for security reasons");
        }

        // No double slashes
        if (basePath.Contains("//"))
        {
            return ValidationResult.Failure("BASE_PATH cannot contain double slashes (//)");
        }

        // Must not end with slash (except root)
        if (basePath.EndsWith('/') && basePath != "/")
        {
            return ValidationResult.Failure("BASE_PATH cannot end with '/' (except root)");
        }

        // Validate format using regex (alphanumeric, hyphens, underscores only)
        if (!ValidPathRegex.IsMatch(basePath))
        {
            return ValidationResult.Failure("BASE_PATH can only contain letters, numbers, hyphens (-), and underscores (_)");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Normalizes a BASE_PATH value (removes trailing slash, trims whitespace)
    /// </summary>
    /// <param name="basePath">The base path to normalize</param>
    /// <returns>Normalized base path or empty string if invalid</returns>
    public static string Normalize(string? basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            return string.Empty;
        }

        basePath = basePath.Trim();

        // Remove trailing slash unless it's just "/"
        if (basePath.EndsWith('/') && basePath != "/")
        {
            basePath = basePath.TrimEnd('/');
        }

        return basePath;
    }
}

/// <summary>
/// Validates PORT values to ensure they are valid and within acceptable ranges
/// </summary>
public static class PortValidator
{
    private const int MinPort = 1;
    private const int MaxPort = 65535;
    private const int DefaultPort = 11011;

    /// <summary>
    /// Validates a PORT value
    /// </summary>
    /// <param name="portString">The port string to validate</param>
    /// <returns>ValidationResult containing success status and error message if invalid</returns>
    public static ValidationResult Validate(string? portString)
    {
        // Null or empty uses default - this is valid
        if (string.IsNullOrWhiteSpace(portString))
        {
            return ValidationResult.Success();
        }

        // Trim whitespace
        portString = portString.Trim();

        // Try to parse as integer
        if (!int.TryParse(portString, out int port))
        {
            return ValidationResult.Failure($"PORT must be a valid integer, got: '{portString}'");
        }

        // Check range
        if (port < MinPort || port > MaxPort)
        {
            return ValidationResult.Failure($"PORT must be between {MinPort} and {MaxPort}, got: {port}");
        }

        // Check for commonly problematic ports
        if (IsWellKnownPort(port))
        {
            return ValidationResult.Failure($"PORT {port} is a well-known system port and should not be used");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Normalizes a PORT value (returns default if null/empty, validates range)
    /// </summary>
    /// <param name="portString">The port string to normalize</param>
    /// <returns>Valid port number or default port</returns>
    public static int Normalize(string? portString)
    {
        if (string.IsNullOrWhiteSpace(portString))
        {
            return DefaultPort;
        }

        portString = portString.Trim();

        if (int.TryParse(portString, out int port))
        {
            // Validate range
            if (port >= MinPort && port <= MaxPort && !IsWellKnownPort(port))
            {
                return port;
            }
        }

        // If invalid, return default
        return DefaultPort;
    }

    /// <summary>
    /// Gets the default port
    /// </summary>
    public static int GetDefaultPort() => DefaultPort;

    /// <summary>
    /// Checks if a port is a well-known system port that should be avoided
    /// </summary>
    private static bool IsWellKnownPort(int port)
    {
        // Common system ports that should be avoided
        int[] wellKnownPorts = { 21, 22, 23, 25, 53, 80, 110, 143, 443, 993, 995 };
        return wellKnownPorts.Contains(port) || port < 1024; // All ports < 1024 are generally reserved
    }
}

/// <summary>
/// Represents the result of a validation operation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; }
    public string ErrorMessage { get; }

    private ValidationResult(bool isValid, string errorMessage = "")
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static ValidationResult Success() => new(true);
    public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
} 