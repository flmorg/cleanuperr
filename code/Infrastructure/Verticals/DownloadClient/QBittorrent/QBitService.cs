using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Attributes;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.QueueCleaner;
using Common.CustomDataTypes;
using Common.Helpers;
using Data.Enums;
using Infrastructure.Configuration;
using Infrastructure.Extensions;
using Infrastructure.Http;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.Files;
using Infrastructure.Verticals.ItemStriker;
using Infrastructure.Verticals.Notifications;
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
        IConfigManager configManager,
        IMemoryCache cache,
        IFilenameEvaluator filenameEvaluator,
        IStriker striker,
        IDryRunInterceptor dryRunInterceptor,
        IHardLinkFileService hardLinkFileService,
        IDynamicHttpClientProvider httpClientProvider,
        EventPublisher eventPublisher,
        BlocklistProvider blocklistProvider
    ) : base(
        logger, configManager, cache, filenameEvaluator, striker, dryRunInterceptor, hardLinkFileService,
        httpClientProvider, eventPublisher, blocklistProvider
    )
    {
        // Client will be initialized when Initialize() is called with a specific client configuration
    }
    
    /// <inheritdoc />
    public override void Initialize(ClientConfig clientConfig)
    {
        // Initialize base service first
        base.Initialize(clientConfig);
        
        // Create QBittorrent client
        _client = new QBittorrentClient(_httpClient, clientConfig.Url);
        
        _logger.LogInformation("Initialized QBittorrent service for client {clientName} ({clientId})", 
            clientConfig.Name, clientConfig.Id);
    }

    public override async Task LoginAsync()
    {
        if (_client == null)
        {
            throw new InvalidOperationException("QBittorrent client is not initialized");
        }
        
        if (string.IsNullOrEmpty(_clientConfig.Username) && string.IsNullOrEmpty(_clientConfig.Password))
        {
            _logger.LogDebug("No credentials configured for client {clientId}, skipping login", _clientConfig.Id);
            return;
        }

        try
        {
            await _client.LoginAsync(_clientConfig.Username, _clientConfig.Password);
            _logger.LogDebug("Successfully logged in to QBittorrent client {clientId}", _clientConfig.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to login to QBittorrent client {clientId}", _clientConfig.Id);
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