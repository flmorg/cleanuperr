using Cleanuparr.Persistence;
using Cleanuparr.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DelugeService = Cleanuparr.Infrastructure.Features.DownloadClient.Deluge.DelugeService;

namespace Cleanuparr.Infrastructure.Http.DynamicHttpClientSystem;

/// <summary>
/// Background service to pre-register standard HttpClient configurations
/// </summary>
public class HttpClientConfigurationService : IHostedService
{
    private readonly IDynamicHttpClientFactory _clientFactory;
    private readonly DataContext _dataContext;
    private readonly ILogger<HttpClientConfigurationService> _logger;

    public HttpClientConfigurationService(
        IDynamicHttpClientFactory clientFactory, 
        DataContext dataContext,
        ILogger<HttpClientConfigurationService> logger)
    {
        _clientFactory = clientFactory;
        _dataContext = dataContext;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var config = await _dataContext.GeneralConfigs
                .AsNoTracking()
                .FirstAsync(cancellationToken);
            
            // Register the retry client (equivalent to Constants.HttpClientWithRetryName)
            _clientFactory.RegisterRetryClient(
                Constants.HttpClientWithRetryName,
                config.HttpTimeout,
                new RetryConfig
                {
                    MaxRetries = config.HttpMaxRetries,
                    ExcludeUnauthorized = true
                },
                config.HttpCertificateValidation
            );

            // Register the Deluge client
            _clientFactory.RegisterDelugeClient(
                nameof(DelugeService),
                config.HttpTimeout,
                new RetryConfig
                {
                    MaxRetries = config.HttpMaxRetries,
                    ExcludeUnauthorized = true
                },
                config.HttpCertificateValidation
            );
            
            _logger.LogInformation("Pre-registered standard HTTP client configurations");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pre-register HTTP client configurations");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
} 