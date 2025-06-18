using Cleanuparr.Persistence.Models.Configuration;

namespace Cleanuparr.Infrastructure.Http;

/// <summary>
/// Interface for a provider that creates HTTP clients dynamically based on client configuration
/// </summary>
public interface IDynamicHttpClientProvider
{
    /// <summary>
    /// Creates an HTTP client configured for the specified download client
    /// </summary>
    /// <param name="downloadClientConfig">The client configuration</param>
    /// <returns>A configured HttpClient instance</returns>
    HttpClient CreateClient(DownloadClientConfig downloadClientConfig);
}
