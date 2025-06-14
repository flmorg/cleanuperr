using Infrastructure.Events;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.Files;
using Infrastructure.Verticals.ItemStriker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Infrastructure.Http;
using Transmission.API.RPC;
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
        IMemoryCache cache,
        IFilenameEvaluator filenameEvaluator,
        IStriker striker,
        IDryRunInterceptor dryRunInterceptor,
        IHardLinkFileService hardLinkFileService,
        IDynamicHttpClientProvider httpClientProvider,
        EventPublisher eventPublisher,
        BlocklistProvider blocklistProvider
    ) : base(
        logger, cache,
        filenameEvaluator, striker, dryRunInterceptor, hardLinkFileService,
        httpClientProvider, eventPublisher, blocklistProvider
    )
    {
        // Client will be initialized when Initialize() is called with a specific client configuration
    }
    
    /// <inheritdoc />
    public override void Initialize(Common.Configuration.DownloadClientConfig downloadClientConfig)
    {
        // Initialize base service first
        base.Initialize(downloadClientConfig);
        
        // Ensure client type is correct
        if (downloadClientConfig.TypeName != Common.Enums.DownloadClientTypeName.Transmission)
        {
            throw new InvalidOperationException($"Cannot initialize TransmissionService with client type {downloadClientConfig.TypeName}");
        }
        
        if (_httpClient == null)
        {
            throw new InvalidOperationException("HTTP client is not initialized");
        }
        
        // Create the RPC path
        string rpcPath = string.IsNullOrEmpty(downloadClientConfig.UrlBase)
            ? "/rpc"
            : $"/{downloadClientConfig.UrlBase.TrimStart('/').TrimEnd('/')}/rpc";
        
        // Create full RPC URL
        string rpcUrl = new UriBuilder(downloadClientConfig.Url) { Path = rpcPath }.Uri.ToString();
        
        // Create Transmission client
        _client = new Client(_httpClient, rpcUrl, login: downloadClientConfig.Username, password: downloadClientConfig.Password);
        
        _logger.LogInformation("Initialized Transmission service for client {clientName} ({clientId})", 
            downloadClientConfig.Name, downloadClientConfig.Id);
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
            _logger.LogDebug("Successfully logged in to Transmission client {clientId}", _downloadClientConfig.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to login to Transmission client {clientId}", _downloadClientConfig.Id);
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