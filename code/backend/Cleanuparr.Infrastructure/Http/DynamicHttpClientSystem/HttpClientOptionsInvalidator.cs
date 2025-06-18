using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cleanuparr.Infrastructure.Http.DynamicHttpClientSystem;

/// <summary>
/// Implementation of HTTP client options invalidator using cache manipulation
/// </summary>
public class HttpClientOptionsInvalidator : IHttpClientOptionsInvalidator
{
    private readonly IOptionsMonitorCache<HttpClientFactoryOptions> _optionsCache;
    private readonly ILogger<HttpClientOptionsInvalidator> _logger;

    public HttpClientOptionsInvalidator(
        IOptionsMonitorCache<HttpClientFactoryOptions> optionsCache,
        ILogger<HttpClientOptionsInvalidator> logger)
    {
        _optionsCache = optionsCache;
        _logger = logger;
    }

    public void InvalidateClient(string clientName)
    {
        try
        {
            // Remove the cached configuration for this specific client
            _optionsCache.TryRemove(clientName);
            
            _logger.LogDebug("Invalidated HTTP client options cache for client: {ClientName}", clientName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate HTTP client options cache for client: {ClientName}", clientName);
        }
    }

    public void InvalidateAllClients()
    {
        try
        {
            // Clear the entire options cache
            _optionsCache.Clear();
            
            _logger.LogDebug("Invalidated all HTTP client options cache entries");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate all HTTP client options cache entries");
        }
    }

    public void InvalidateClients(IEnumerable<string> clientNames)
    {
        var clientNamesList = clientNames.ToList();
        
        try
        {
            foreach (var clientName in clientNamesList)
            {
                _optionsCache.TryRemove(clientName);
            }
            
            _logger.LogDebug("Invalidated HTTP client options cache for {Count} clients: {ClientNames}", 
                clientNamesList.Count, string.Join(", ", clientNamesList));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate HTTP client options cache for multiple clients");
        }
    }
} 