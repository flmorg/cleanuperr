using Common.Configuration.DownloadClient;
using Common.Exceptions;
using Data.Models.Deluge.Response;
using Infrastructure.Events;
using Infrastructure.Interceptors;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.Files;
using Infrastructure.Verticals.ItemStriker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Infrastructure.Configuration;
using Infrastructure.Http;

namespace Infrastructure.Verticals.DownloadClient.Deluge;

public partial class DelugeService : DownloadService, IDelugeService
{
    private DelugeClient? _client;

    public DelugeService(
        ILogger<DelugeService> logger,
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
        // TODO initialize client & httpclient here
    }
    
    /// <inheritdoc />
    public override void Initialize(ClientConfig clientConfig)
    {
        // Initialize base service first
        base.Initialize(clientConfig);
        
        // Ensure client type is correct
        if (clientConfig.Type != Common.Enums.DownloadClientType.Deluge)
        {
            throw new InvalidOperationException($"Cannot initialize DelugeService with client type {clientConfig.Type}");
        }
        
        if (_httpClient == null)
        {
            throw new InvalidOperationException("HTTP client is not initialized");
        }
        
        // Create Deluge client
        _client = new DelugeClient(clientConfig, _httpClient);
        
        _logger.LogInformation("Initialized Deluge service for client {clientName} ({clientId})", 
            clientConfig.Name, clientConfig.Id);
    }
    
    public override async Task LoginAsync()
    {
        if (_client == null)
        {
            throw new InvalidOperationException("Deluge client is not initialized");
        }
        
        try 
        {
            await _client.LoginAsync();
            
            if (!await _client.IsConnected() && !await _client.Connect())
            {
                throw new FatalException("Deluge WebUI is not connected to the daemon");
            }
            
            _logger.LogDebug("Successfully logged in to Deluge client {clientId}", _clientConfig.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to login to Deluge client {clientId}", _clientConfig.Id);
            throw;
        }
    }
    
    private static void ProcessFiles(Dictionary<string, DelugeFileOrDirectory>? contents, Action<string, DelugeFileOrDirectory> processFile)
    {
        if (contents is null)
        {
            return;
        }
        
        foreach (var (name, data) in contents)
        {
            switch (data.Type)
            {
                case "file":
                    processFile(name, data);
                    break;
                case "dir" when data.Contents is not null:
                    // Recurse into subdirectories
                    ProcessFiles(data.Contents, processFile);
                    break;
            }
        }
    }

    public override void Dispose()
    {
        _client = null;
        _httpClient?.Dispose();
    }
}