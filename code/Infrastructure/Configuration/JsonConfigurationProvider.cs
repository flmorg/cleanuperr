using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Configuration;

/// <summary>
/// Provides thread-safe access to JSON configuration files.
/// </summary>
public class JsonConfigurationProvider : IConfigurationProvider
{
    private readonly ILogger<JsonConfigurationProvider> _logger;
    private readonly string _configDirectory;
    private readonly Dictionary<string, SemaphoreSlim> _fileLocks = new();
    private readonly JsonSerializerOptions _serializerOptions;

    public JsonConfigurationProvider(ILogger<JsonConfigurationProvider> logger, ConfigurationPathProvider pathProvider)
    {
        _logger = logger;
        _configDirectory = pathProvider.GetSettingsPath();
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(_configDirectory))
        {
            try
            {
                Directory.CreateDirectory(_configDirectory);
                _logger.LogInformation("Created configuration directory: {directory}", _configDirectory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create configuration directory: {directory}", _configDirectory);
                throw;
            }
        }

        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
        _serializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    /// <summary>
    /// Gets the lock object for a specific file, creating it if necessary.
    /// </summary>
    private SemaphoreSlim GetFileLock(string fileName)
    {
        if (_fileLocks.TryGetValue(fileName, out var semaphore))
        {
            return semaphore;
        }

        semaphore = new SemaphoreSlim(1, 1);
        _fileLocks[fileName] = semaphore;
        return semaphore;
    }

    /// <summary>
    /// Gets the full path to a configuration file.
    /// </summary>
    private string GetFullPath(string fileName)
    {
        return Path.Combine(_configDirectory, fileName);
    }

    /// <summary>
    /// Reads a configuration from a JSON file asynchronously.
    /// </summary>
    public async Task<T> ReadConfigurationAsync<T>(string fileName) where T : class, new()
    {
        var fileLock = GetFileLock(fileName);
        var fullPath = GetFullPath(fileName);

        try
        {
            await fileLock.WaitAsync();

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Configuration file does not exist: {file}", fullPath);
                return new T();
            }

            var json = await File.ReadAllTextAsync(fullPath);

            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("Configuration file is empty: {file}", fullPath);
                return new T();
            }

            var config = JsonSerializer.Deserialize<T>(json, _serializerOptions);
            if (config == null)
            {
                _logger.LogWarning("Failed to deserialize configuration: {file}", fullPath);
                return new T();
            }
            
            _logger.LogDebug("Read configuration from {file}", fullPath);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading configuration from {file}", fullPath);
            return new T();
        }
        finally
        {
            fileLock.Release();
        }
    }

    /// <summary>
    /// Reads a configuration from a JSON file synchronously.
    /// </summary>
    public T ReadConfiguration<T>(string fileName) where T : class, new()
    {
        var fileLock = GetFileLock(fileName);
        var fullPath = GetFullPath(fileName);

        try
        {
            fileLock.Wait();

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Configuration file does not exist: {file}", fullPath);
                return new T();
            }

            var json = File.ReadAllText(fullPath);

            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("Configuration file is empty: {file}", fullPath);
                return new T();
            }

            var config = JsonSerializer.Deserialize<T>(json, _serializerOptions);
            if (config == null)
            {
                _logger.LogWarning("Failed to deserialize configuration: {file}", fullPath);
                return new T();
            }
            
            _logger.LogDebug("Read configuration from {file}", fullPath);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading configuration from {file}", fullPath);
            return new T();
        }
        finally
        {
            fileLock.Release();
        }
    }

    /// <summary>
    /// Writes a configuration to a JSON file in a thread-safe manner asynchronously.
    /// </summary>
    public async Task<bool> WriteConfigurationAsync<T>(string fileName, T configuration) where T : class
    {
        var fileLock = GetFileLock(fileName);
        var fullPath = GetFullPath(fileName);

        try
        {
            await fileLock.WaitAsync();

            // Create backup if file exists
            if (File.Exists(fullPath))
            {
                var backupPath = $"{fullPath}.bak";
                try
                {
                    File.Copy(fullPath, backupPath, true);
                    _logger.LogDebug("Created backup of configuration file: {backup}", backupPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create backup of configuration file: {file}", fullPath);
                    // Continue anyway - prefer having new config to having no config
                }
            }

            var json = JsonSerializer.Serialize(configuration, _serializerOptions);
            await File.WriteAllTextAsync(fullPath, json);
            
            _logger.LogInformation("Wrote configuration to {file}", fullPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing configuration to {file}", fullPath);
            return false;
        }
        finally
        {
            fileLock.Release();
        }
    }

    /// <summary>
    /// Writes a configuration to a JSON file in a thread-safe manner synchronously.
    /// </summary>
    public bool WriteConfiguration<T>(string fileName, T configuration) where T : class
    {
        var fileLock = GetFileLock(fileName);
        var fullPath = GetFullPath(fileName);

        try
        {
            fileLock.Wait();

            // Create backup if file exists
            if (File.Exists(fullPath))
            {
                var backupPath = $"{fullPath}.bak";
                try
                {
                    File.Copy(fullPath, backupPath, true);
                    _logger.LogDebug("Created backup of configuration file: {backup}", backupPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create backup of configuration file: {file}", fullPath);
                    // Continue anyway - prefer having new config to having no config
                }
            }

            var json = JsonSerializer.Serialize(configuration, _serializerOptions);
            File.WriteAllText(fullPath, json);
            
            _logger.LogInformation("Wrote configuration to {file}", fullPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing configuration to {file}", fullPath);
            return false;
        }
        finally
        {
            fileLock.Release();
        }
    }

    /// <summary>
    /// Updates a specific property within a JSON configuration file.
    /// </summary>
    public async Task<bool> UpdateConfigurationPropertyAsync<T>(string fileName, string propertyPath, T value)
    {
        var fileLock = GetFileLock(fileName);
        var fullPath = GetFullPath(fileName);

        try
        {
            await fileLock.WaitAsync();

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Configuration file does not exist: {file}", fullPath);
                return false;
            }

            // Create backup
            var backupPath = $"{fullPath}.bak";
            try
            {
                File.Copy(fullPath, backupPath, true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create backup of configuration file: {file}", fullPath);
            }

            var json = await File.ReadAllTextAsync(fullPath);
            var jsonNode = JsonNode.Parse(json)?.AsObject();

            if (jsonNode == null)
            {
                _logger.LogError("Failed to parse configuration file: {file}", fullPath);
                return false;
            }

            // Handle simple property paths like "propertyName"
            if (!propertyPath.Contains('.'))
            {
                jsonNode[propertyPath] = JsonValue.Create(value);
            }
            else
            {
                // Handle nested property paths like "parent.child.property"
                var parts = propertyPath.Split('.');
                var current = jsonNode;

                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (current[parts[i]] is JsonObject nestedObject)
                    {
                        current = nestedObject;
                    }
                    else
                    {
                        var newObject = new JsonObject();
                        current[parts[i]] = newObject;
                        current = newObject;
                    }
                }

                current[parts[^1]] = JsonValue.Create(value);
            }

            var updatedJson = jsonNode.ToJsonString(_serializerOptions);
            await File.WriteAllTextAsync(fullPath, updatedJson);
            
            _logger.LogInformation("Updated property {property} in {file}", propertyPath, fullPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating property {property} in {file}", propertyPath, fullPath);
            return false;
        }
        finally
        {
            fileLock.Release();
        }
    }
    
    /// <summary>
    /// Updates a specific property within a JSON configuration file synchronously.
    /// </summary>
    public bool UpdateConfigurationProperty<T>(string fileName, string propertyPath, T value)
    {
        var fileLock = GetFileLock(fileName);
        var fullPath = GetFullPath(fileName);

        try
        {
            fileLock.Wait();

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Configuration file does not exist: {file}", fullPath);
                return false;
            }

            // Create backup
            var backupPath = $"{fullPath}.bak";
            try
            {
                File.Copy(fullPath, backupPath, true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create backup of configuration file: {file}", fullPath);
            }

            var json = File.ReadAllText(fullPath);
            var jsonNode = JsonNode.Parse(json)?.AsObject();

            if (jsonNode == null)
            {
                _logger.LogError("Failed to parse configuration file: {file}", fullPath);
                return false;
            }

            // Handle simple property paths like "propertyName"
            if (!propertyPath.Contains('.'))
            {
                jsonNode[propertyPath] = JsonValue.Create(value);
            }
            else
            {
                // Handle nested property paths like "parent.child.property"
                var parts = propertyPath.Split('.');
                var current = jsonNode;

                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (current[parts[i]] is JsonObject nestedObject)
                    {
                        current = nestedObject;
                    }
                    else
                    {
                        var newObject = new JsonObject();
                        current[parts[i]] = newObject;
                        current = newObject;
                    }
                }

                current[parts[^1]] = JsonValue.Create(value);
            }

            var updatedJson = jsonNode.ToJsonString(_serializerOptions);
            File.WriteAllText(fullPath, updatedJson);
            
            _logger.LogInformation("Updated property {property} in {file}", propertyPath, fullPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating property {property} in {file}", propertyPath, fullPath);
            return false;
        }
        finally
        {
            fileLock.Release();
        }
    }

    /// <summary>
    /// Merges an existing configuration with new values.
    /// </summary>
    public async Task<bool> MergeConfigurationAsync<T>(string fileName, T newValues) where T : class
    {
        var fileLock = GetFileLock(fileName);
        var fullPath = GetFullPath(fileName);

        try
        {
            await fileLock.WaitAsync();

            T currentConfig;

            if (File.Exists(fullPath))
            {
                var json = await File.ReadAllTextAsync(fullPath);
                currentConfig = JsonSerializer.Deserialize<T>(json, _serializerOptions) ?? Activator.CreateInstance<T>();

                // Create backup
                var backupPath = $"{fullPath}.bak";
                try
                {
                    File.Copy(fullPath, backupPath, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create backup of configuration file: {file}", fullPath);
                }
            }
            else
            {
                currentConfig = Activator.CreateInstance<T>() ?? throw new InvalidOperationException($"Failed to create instance of {typeof(T).Name}");
            }

            // Merge properties using JsonNode
            var currentJson = JsonSerializer.Serialize(currentConfig, _serializerOptions);
            var currentNode = JsonNode.Parse(currentJson)?.AsObject();
            
            var newJson = JsonSerializer.Serialize(newValues, _serializerOptions);
            var newNode = JsonNode.Parse(newJson)?.AsObject();

            if (currentNode == null || newNode == null)
            {
                _logger.LogError("Failed to parse configuration for merging: {file}", fullPath);
                return false;
            }

            MergeJsonNodes(currentNode, newNode);

            var mergedJson = currentNode.ToJsonString(_serializerOptions);
            await File.WriteAllTextAsync(fullPath, mergedJson);
            
            _logger.LogInformation("Merged configuration in {file}", fullPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging configuration in {file}", fullPath);
            return false;
        }
        finally
        {
            fileLock.Release();
        }
    }
    
    /// <summary>
    /// Merges an existing configuration with new values synchronously.
    /// </summary>
    public bool MergeConfiguration<T>(string fileName, T newValues) where T : class
    {
        var fileLock = GetFileLock(fileName);
        var fullPath = GetFullPath(fileName);

        try
        {
            fileLock.Wait();

            T currentConfig;

            if (File.Exists(fullPath))
            {
                var json = File.ReadAllText(fullPath);
                currentConfig = JsonSerializer.Deserialize<T>(json, _serializerOptions) ?? Activator.CreateInstance<T>();

                // Create backup
                var backupPath = $"{fullPath}.bak";
                try
                {
                    File.Copy(fullPath, backupPath, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create backup of configuration file: {file}", fullPath);
                }
            }
            else
            {
                currentConfig = Activator.CreateInstance<T>() ?? throw new InvalidOperationException($"Failed to create instance of {typeof(T).Name}");
            }

            // Merge properties using JsonNode
            var currentJson = JsonSerializer.Serialize(currentConfig, _serializerOptions);
            var currentNode = JsonNode.Parse(currentJson)?.AsObject();
            
            var newJson = JsonSerializer.Serialize(newValues, _serializerOptions);
            var newNode = JsonNode.Parse(newJson)?.AsObject();

            if (currentNode == null || newNode == null)
            {
                _logger.LogError("Failed to parse configuration for merging: {file}", fullPath);
                return false;
            }

            MergeJsonNodes(currentNode, newNode);

            var mergedJson = currentNode.ToJsonString(_serializerOptions);
            File.WriteAllText(fullPath, mergedJson);
            
            _logger.LogInformation("Merged configuration in {file}", fullPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging configuration in {file}", fullPath);
            return false;
        }
        finally
        {
            fileLock.Release();
        }
    }

    private void MergeJsonNodes(JsonObject target, JsonObject source)
    {
        foreach (var property in source)
        {
            if (property.Value is JsonObject sourceObject)
            {
                if (target[property.Key] is JsonObject targetObject)
                {
                    // Recursively merge nested objects
                    MergeJsonNodes(targetObject, sourceObject);
                }
                else
                {
                    // Replace with new object
                    target[property.Key] = sourceObject.DeepClone();
                }
            }
            else
            {
                // Replace value
                target[property.Key] = property.Value?.DeepClone();
            }
        }
    }

    /// <summary>
    /// Deletes a configuration file.
    /// </summary>
    public async Task<bool> DeleteConfigurationAsync(string fileName)
    {
        var fileLock = GetFileLock(fileName);
        var fullPath = GetFullPath(fileName);

        try
        {
            await fileLock.WaitAsync();

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Configuration file does not exist: {file}", fullPath);
                return true; // Already gone
            }

            // Create backup
            var backupPath = $"{fullPath}.bak";
            try
            {
                File.Copy(fullPath, backupPath, true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create backup of configuration file: {file}", fullPath);
            }

            File.Delete(fullPath);
            _logger.LogInformation("Deleted configuration file: {file}", fullPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration file: {file}", fullPath);
            return false;
        }
        finally
        {
            fileLock.Release();
        }
    }

    /// <summary>
    /// Deletes a configuration file synchronously.
    /// </summary>
    public bool DeleteConfiguration(string fileName)
    {
        var fileLock = GetFileLock(fileName);
        var fullPath = GetFullPath(fileName);

        try
        {
            fileLock.Wait();

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Configuration file does not exist: {file}", fullPath);
                return true; // Already gone
            }

            // Create backup
            var backupPath = $"{fullPath}.bak";
            try
            {
                File.Copy(fullPath, backupPath, true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create backup of configuration file: {file}", fullPath);
            }

            File.Delete(fullPath);
            _logger.LogInformation("Deleted configuration file: {file}", fullPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration file: {file}", fullPath);
            return false;
        }
        finally
        {
            fileLock.Release();
        }
    }

    /// <summary>
    /// Lists all configuration files in the configuration directory.
    /// </summary>
    public IEnumerable<string> ListConfigurationFiles()
    {
        try
        {
            return Directory.GetFiles(_configDirectory, "*.json")
                .Select(Path.GetFileName)
                .Where(f => !f.EndsWith(".bak"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing configuration files in {directory}", _configDirectory);
            return Enumerable.Empty<string>();
        }
    }
}
