using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Attributes;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.DownloadClient;
using Common.Configuration.QueueCleaner;
using Common.CustomDataTypes;
using Common.Helpers;
using Data.Enums;
using Infrastructure.Events;
using Infrastructure.Extensions;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.Files;
using Infrastructure.Verticals.ItemStriker;
using Infrastructure.Verticals.Notifications;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Infrastructure.Configuration;
using Infrastructure.Http;
using Transmission.API.RPC;
using Transmission.API.RPC.Arguments;
using Transmission.API.RPC.Entity;

namespace Infrastructure.Verticals.DownloadClient.Transmission;

public partial class TransmissionService : DownloadService, ITransmissionService
{
    private Client? _client;

    private static readonly string[] Fields =
    [
        TorrentFields.FILES,
        TorrentFields.FILE_STATS,
        TorrentFields.HASH_STRING,
        TorrentFields.ID,
        TorrentFields.ETA,
        TorrentFields.NAME,
        TorrentFields.STATUS,
        TorrentFields.IS_PRIVATE,
        TorrentFields.DOWNLOADED_EVER,
        TorrentFields.DOWNLOAD_DIR,
        TorrentFields.SECONDS_SEEDING,
        TorrentFields.UPLOAD_RATIO,
        TorrentFields.TRACKERS,
        TorrentFields.RATE_DOWNLOAD,
        TorrentFields.TOTAL_SIZE,
    ];

    public TransmissionService(
        ILogger<TransmissionService> logger,
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
        logger, configManager, cache,
        filenameEvaluator, striker, dryRunInterceptor, hardLinkFileService,
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
        
        // Ensure client type is correct
        if (clientConfig.Type != Common.Enums.DownloadClientType.Transmission)
        {
            throw new InvalidOperationException($"Cannot initialize TransmissionService with client type {clientConfig.Type}");
        }
        
        if (_httpClient == null)
        {
            throw new InvalidOperationException("HTTP client is not initialized");
        }
        
        // Create the RPC path
        string rpcPath = string.IsNullOrEmpty(clientConfig.UrlBase)
            ? "/rpc"
            : $"/{clientConfig.UrlBase.TrimStart('/').TrimEnd('/')}/rpc";
        
        // Create full RPC URL
        string rpcUrl = new UriBuilder(clientConfig.Url) { Path = rpcPath }.Uri.ToString();
        
        // Create Transmission client
        _client = new Client(_httpClient, rpcUrl, login: clientConfig.Username, password: clientConfig.Password);
        
        _logger.LogInformation("Initialized Transmission service for client {clientName} ({clientId})", 
            clientConfig.Name, clientConfig.Id);
    }

    public override async Task LoginAsync()
    {
        if (_client == null)
        {
            throw new InvalidOperationException("Transmission client is not initialized");
        }
        
        try 
        {
            await _client.GetSessionInformationAsync();
            _logger.LogDebug("Successfully logged in to Transmission client {clientId}", _clientConfig.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to login to Transmission client {clientId}", _clientConfig.Id);
            throw;
        }
    }
    
    public override void Dispose()
    {
        _client = null;
        _httpClient?.Dispose();
    }

    private async Task<TorrentInfo?> GetTorrentAsync(string hash)
    {
        return (await _client.TorrentGetAsync(Fields, hash))
            ?.Torrents
            ?.FirstOrDefault();
    }
}