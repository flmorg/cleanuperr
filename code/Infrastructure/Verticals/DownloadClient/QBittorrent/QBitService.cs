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
    protected QBittorrentClient? _client;

    public QBitService(
        ILogger<QBitService> logger,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IFilenameEvaluator filenameEvaluator,
        IStriker striker,
        IDryRunInterceptor dryRunInterceptor,
        IHardLinkFileService hardLinkFileService,
        IDynamicHttpClientProvider httpClientProvider,
        EventPublisher eventPublisher,
        BlocklistProvider blocklistProvider
    ) : base(
        logger, cache, filenameEvaluator, striker, dryRunInterceptor, hardLinkFileService,
        httpClientProvider, eventPublisher, blocklistProvider
    )
    {
        // Client will be initialized when Initialize() is called with a specific client configuration
    }
    
    /// <inheritdoc />
    public override void Initialize(DownloadClientConfig downloadClientConfig)
    {
        // Initialize base service first
        base.Initialize(downloadClientConfig);
        
        // Create QBittorrent client
        _client = new QBittorrentClient(_httpClient, downloadClientConfig.Url);
        
        _logger.LogInformation("Initialized QBittorrent service for client {clientName} ({clientId})", 
            downloadClientConfig.Name, downloadClientConfig.Id);
    }

    public override async Task LoginAsync()
    {
        if (_client == null)
        {
            throw new InvalidOperationException("QBittorrent client is not initialized");
        }
        
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
    
    private async Task<IReadOnlyList<TorrentTracker>> GetTrackersAsync(string hash)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("QBittorrent client is not initialized");
        }
        
        return (await _client.GetTorrentTrackersAsync(hash))
            .Where(x => x.Url.Contains("**"))
            .ToList();
    }
    
    public override void Dispose()
    {
        _client?.Dispose();
        _httpClient?.Dispose();
    }
}