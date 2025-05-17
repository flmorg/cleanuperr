using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Configuration;

/// <summary>
/// Interface for configuration providers
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>
    /// Reads a configuration from storage asynchronously
    /// </summary>
    Task<T?> ReadConfigurationAsync<T>(string fileName) where T : class, new();
    
    /// <summary>
    /// Reads a configuration from storage synchronously
    /// </summary>
    T? ReadConfiguration<T>(string fileName) where T : class, new();
    
    /// <summary>
    /// Writes a configuration to storage asynchronously
    /// </summary>
    Task<bool> WriteConfigurationAsync<T>(string fileName, T configuration) where T : class;
    
    /// <summary>
    /// Writes a configuration to storage synchronously
    /// </summary>
    bool WriteConfiguration<T>(string fileName, T configuration) where T : class;
    
    /// <summary>
    /// Updates a specific property in a configuration asynchronously
    /// </summary>
    Task<bool> UpdateConfigurationPropertyAsync<T>(string fileName, string propertyPath, T value);
    
    /// <summary>
    /// Updates a specific property in a configuration synchronously
    /// </summary>
    bool UpdateConfigurationProperty<T>(string fileName, string propertyPath, T value);
    
    /// <summary>
    /// Merges configuration values asynchronously
    /// </summary>
    Task<bool> MergeConfigurationAsync<T>(string fileName, T newValues) where T : class;
    
    /// <summary>
    /// Merges configuration values synchronously
    /// </summary>
    bool MergeConfiguration<T>(string fileName, T newValues) where T : class;
    
    /// <summary>
    /// Deletes a configuration asynchronously
    /// </summary>
    Task<bool> DeleteConfigurationAsync(string fileName);
    
    /// <summary>
    /// Deletes a configuration synchronously
    /// </summary>
    bool DeleteConfiguration(string fileName);
    
    /// <summary>
    /// Lists all available configuration files
    /// </summary>
    IEnumerable<string> ListConfigurationFiles();
}
