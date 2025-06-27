using System.Collections.Concurrent;

namespace Cleanuparr.Infrastructure.Http.DynamicHttpClientSystem;

/// <summary>
/// In-memory implementation of the HTTP client configuration store
/// </summary>
public class HttpClientConfigStore : IHttpClientConfigStore
{
    private readonly ConcurrentDictionary<string, HttpClientConfig> _configurations = new();
    private readonly ConcurrentDictionary<string, RetryConfig> _retryConfigurations = new();

    public bool TryGetConfiguration(string clientName, out HttpClientConfig config)
    {
        return _configurations.TryGetValue(clientName, out config!);
    }

    public void AddConfiguration(string clientName, HttpClientConfig config)
    {
        _configurations.AddOrUpdate(clientName, config, (_, _) => config);
    }

    public void RemoveConfiguration(string clientName)
    {
        _configurations.TryRemove(clientName, out _);
        _retryConfigurations.TryRemove(clientName, out _);
    }

    public void AddRetryConfiguration(string clientName, RetryConfig retryConfig)
    {
        _retryConfigurations.AddOrUpdate(clientName, retryConfig, (key, oldValue) => retryConfig);
    }

    public bool TryGetRetryConfiguration(string clientName, out RetryConfig retryConfig)
    {
        return _retryConfigurations.TryGetValue(clientName, out retryConfig!);
    }

    public IEnumerable<KeyValuePair<string, HttpClientConfig>> GetAllConfigurations()
    {
        return _configurations.ToList(); // Return a snapshot to avoid collection modification issues
    }

    public void UpdateConfigurations(IEnumerable<KeyValuePair<string, HttpClientConfig>> configurations)
    {
        foreach (var kvp in configurations)
        {
            _configurations.AddOrUpdate(kvp.Key, kvp.Value, (_, _) => kvp.Value);
        }
    }
} 