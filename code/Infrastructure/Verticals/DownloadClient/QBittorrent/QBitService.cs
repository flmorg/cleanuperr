using Common.Configuration;
using Data;
using Infrastructure.Http;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.Files;
using Infrastructure.Verticals.ItemStriker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QBittorrent.Client;
using Infrastructure.Events;

namespace Infrastructure.Verticals.DownloadClient.QBittorrent;

public partial class QBitService : DownloadService, IQBitService
{
    protected readonly QBittorrentClient _client;

    public QBitService(
        ILogger<QBitService> logger,
        IMemoryCache cache,
        IFilenameEvaluator filenameEvaluator,
        IStriker striker,
        IDryRunInterceptor dryRunInterceptor,
        IHardLinkFileService hardLinkFileService,
        IDynamicHttpClientProvider httpClientProvider,
        EventPublisher eventPublisher,
        BlocklistProvider blocklistProvider,
        DownloadClientConfig downloadClientConfig
    ) : base(
        logger, cache, filenameEvaluator, striker, dryRunInterceptor, hardLinkFileService,
        httpClientProvider, eventPublisher, blocklistProvider, downloadClientConfig
    )
    {
        _client = new QBittorrentClient(_httpClient, downloadClientConfig.Url);
    }
    
    public override async Task LoginAsync()
    {
        if (string.IsNullOrEmpty(_downloadClientConfig.Username) && string.IsNullOrEmpty(_downloadClientConfig.Password))
        {
            _logger.LogDebug("No credentials configured for client {clientId}, skipping login", _downloadClientConfig.Id);
            return;
        }

        try
        {
            await _client.LoginAsync(_downloadClientConfig.Username, _downloadClientConfig.Password);
            _logger.LogDebug("Successfully logged in to QBittorrent client {clientId}", _downloadClientConfig.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to login to QBittorrent client {clientId}", _downloadClientConfig.Id);
            throw;
        }
    }

    public override async Task<HealthCheckResult> HealthCheckAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            bool hasCredentials = !string.IsNullOrEmpty(_downloadClientConfig.Username) || 
                                  !string.IsNullOrEmpty(_downloadClientConfig.Password);

            if (hasCredentials)
            {
                // If credentials are provided, we must be able to login for the service to be healthy
                await _client.LoginAsync(_downloadClientConfig.Username, _downloadClientConfig.Password);
                _logger.LogDebug("Health check: Successfully logged in to QBittorrent client {clientId}", _downloadClientConfig.Id);
            }
            else
            {
                // If no credentials, test connectivity using version endpoint
                await _client.GetApiVersionAsync();
                _logger.LogDebug("Health check: Successfully connected to QBittorrent client {clientId}", _downloadClientConfig.Id);
            }

            stopwatch.Stop();

            return new HealthCheckResult
            {
                IsHealthy = true,
                ResponseTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogWarning(ex, "Health check failed for QBittorrent client {clientId}", _downloadClientConfig.Id);
            
            return new HealthCheckResult
            {
                IsHealthy = false,
                ErrorMessage = $"Connection failed: {ex.Message}",
                ResponseTime = stopwatch.Elapsed
            };
        }
    }
    
    private async Task<IReadOnlyList<TorrentTracker>> GetTrackersAsync(string hash)
    {
        return (await _client.GetTorrentTrackersAsync(hash))
            .Where(x => x.Url.Contains("**"))
            .ToList();
    }
    
    public override void Dispose()
    {
        _client.Dispose();
    }
}