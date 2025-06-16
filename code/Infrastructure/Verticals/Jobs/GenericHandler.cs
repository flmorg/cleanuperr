using Common.Configuration;
using Common.Enums;
using Data.Models.Configuration.Arr;
using Data.Models.Configuration.DownloadCleaner;
using Data.Models.Configuration.General;
using Data.Models.Configuration.QueueCleaner;
using Data;
using Data.Enums;
using Data.Models.Arr;
using Data.Models.Arr.Queue;
using Data.Models.Configuration.Arr;
using Infrastructure.Events;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.Context;
using Infrastructure.Verticals.DownloadClient;
using Infrastructure.Verticals.DownloadRemover.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.Jobs;

public abstract class GenericHandler : IHandler
{
    protected readonly ILogger<GenericHandler> _logger;
    protected readonly DataContext _dataContext;
    protected readonly IMemoryCache _cache;
    protected readonly IBus _messageBus;
    protected readonly ArrClientFactory _arrClientFactory;
    protected readonly ArrQueueIterator _arrArrQueueIterator;
    protected readonly DownloadServiceFactory _downloadServiceFactory;
    private readonly EventPublisher _eventPublisher;

    protected GenericHandler(
        ILogger<GenericHandler> logger,
        DataContext dataContext,
        IMemoryCache cache,
        IBus messageBus,
        ArrClientFactory arrClientFactory,
        ArrQueueIterator arrArrQueueIterator,
        DownloadServiceFactory downloadServiceFactory,
        EventPublisher eventPublisher
    )
    {
        _logger = logger;
        _cache = cache;
        _messageBus = messageBus;
        _arrClientFactory = arrClientFactory;
        _arrArrQueueIterator = arrArrQueueIterator;
        _downloadServiceFactory = downloadServiceFactory;
        _eventPublisher = eventPublisher;
        _dataContext = dataContext;
    }

    /// <summary>
    /// Initialize download services based on configuration
    /// </summary>
    protected async Task<List<IDownloadService>> GetDownloadServices()
    {
        var clients = await _dataContext.DownloadClients
            .AsNoTracking()
            .ToListAsync();
        
        if (clients.Count is 0)
        {
            _logger.LogWarning("No download clients configured");
            return [];
        }
        
        var enabledClients = await _dataContext.DownloadClients
            .Where(c => c.Enabled)
            .ToListAsync();

        if (enabledClients.Count == 0)
        {
            _logger.LogWarning("No enabled download clients available");
            return [];
        }
        
        List<IDownloadService> downloadServices = [];
        
        // Add all enabled clients
        foreach (var client in enabledClients)
        {
            try
            {
                var service = _downloadServiceFactory.GetDownloadService(client);
                if (service != null)
                {
                    await service.LoginAsync();
                    downloadServices.Add(service);
                    _logger.LogDebug("Initialized download client: {name}", client.Name);
                }
                else
                {
                    _logger.LogWarning("Download client service not available for: {name}", client.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize download client: {name}", client.Name);
            }
        }
        
        if (downloadServices.Count == 0)
        {
            _logger.LogWarning("No valid download clients found");
        }
        else
        {
            _logger.LogDebug("Initialized {count} download clients", downloadServices.Count);
        }

        return downloadServices;
    }

    public async Task ExecuteAsync()
    {
        await DataContext.Lock.WaitAsync();

        try
        {
            ContextProvider.Set(nameof(GeneralConfig), await _dataContext.GeneralConfigs.FirstAsync());
            ContextProvider.Set(nameof(InstanceType.Sonarr), await _dataContext.ArrConfigs
                .Include(x => x.Instances)
                .FirstAsync(x => x.Type == InstanceType.Sonarr));
            ContextProvider.Set(nameof(InstanceType.Radarr), await _dataContext.ArrConfigs
                .Include(x => x.Instances)
                .FirstAsync(x => x.Type == InstanceType.Radarr));
            ContextProvider.Set(nameof(InstanceType.Lidarr), await _dataContext.ArrConfigs
                .Include(x => x.Instances)
                .FirstAsync(x => x.Type == InstanceType.Lidarr));
            ContextProvider.Set(nameof(QueueCleanerConfig), await _dataContext.QueueCleanerConfigs.FirstAsync());
            ContextProvider.Set(nameof(DownloadCleanerConfig), await _dataContext.DownloadCleanerConfigs.FirstAsync());
            ContextProvider.Set(nameof(DownloadClientConfig), await _dataContext.DownloadClients
                .Where(x => x.Enabled)
                .ToListAsync());
        }
        finally
        {
            DataContext.Lock.Release();
        }
        
        await ExecuteInternalAsync();
    }

    protected abstract Task ExecuteInternalAsync();
    
    protected abstract Task ProcessInstanceAsync(ArrInstance instance, InstanceType instanceType, ArrConfig arrConfig);
    
    protected async Task ProcessArrConfigAsync(ArrConfig config, InstanceType instanceType, bool throwOnFailure = false)
    {
        if (!config.Enabled)
        {
            return;
        }

        foreach (ArrInstance arrInstance in config.Instances)
        {
            try
            {
                await ProcessInstanceAsync(arrInstance, instanceType, config);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "failed to process {type} instance | {url}", instanceType, arrInstance.Url);

                if (throwOnFailure)
                {
                    throw;
                }
            }
        }
    }

    protected async Task PublishQueueItemRemoveRequest(
        string downloadRemovalKey,
        InstanceType instanceType,
        ArrInstance instance,
        QueueRecord record,
        bool isPack,
        bool removeFromClient,
        DeleteReason deleteReason
    )
    {
        if (instanceType is InstanceType.Sonarr)
        {
            QueueItemRemoveRequest<SonarrSearchItem> removeRequest = new()
            {
                InstanceType = instanceType,
                Instance = instance,
                Record = record,
                SearchItem = (SonarrSearchItem)GetRecordSearchItem(instanceType, record, isPack),
                RemoveFromClient = removeFromClient,
                DeleteReason = deleteReason
            };

            await _messageBus.Publish(removeRequest);
        }
        else
        {
            QueueItemRemoveRequest<SearchItem> removeRequest = new()
            {
                InstanceType = instanceType,
                Instance = instance,
                Record = record,
                SearchItem = GetRecordSearchItem(instanceType, record, isPack),
                RemoveFromClient = removeFromClient,
                DeleteReason = deleteReason
            };

            await _messageBus.Publish(removeRequest);
        }

        _logger.LogInformation("item marked for removal | {title} | {url}", record.Title, instance.Url);
        await _eventPublisher.PublishAsync(EventType.DownloadMarkedForDeletion, "Download marked for deletion", EventSeverity.Important);
    }
    
    protected SearchItem GetRecordSearchItem(InstanceType type, QueueRecord record, bool isPack = false)
    {
        return type switch
        {
            InstanceType.Sonarr when !isPack => new SonarrSearchItem
            {
                Id = record.EpisodeId,
                SeriesId = record.SeriesId,
                SearchType = SonarrSearchType.Episode
            },
            InstanceType.Sonarr when isPack => new SonarrSearchItem
            {
                Id = record.SeasonNumber,
                SeriesId = record.SeriesId,
                SearchType = SonarrSearchType.Season
            },
            InstanceType.Radarr => new SearchItem
            {
                Id = record.MovieId
            },
            InstanceType.Lidarr => new SearchItem
            {
                Id = record.AlbumId
            },
            _ => throw new NotImplementedException($"instance type {type} is not yet supported")
        };
    }

    protected async Task<IReadOnlyList<IDownloadService>> GetInitializedDownloadServicesAsync()
    {
        var downloadClientConfigs = ContextProvider.Get<List<DownloadClientConfig>>(nameof(DownloadClientConfig));
        List<IDownloadService> downloadServices = [];

        foreach (var config in downloadClientConfigs)
        {
            if (config.TypeName is DownloadClientTypeName.Usenet)
            {
                continue;
            }

            try
            {
                var downloadService = _downloadServiceFactory.GetDownloadService(config);
                await downloadService.LoginAsync();
                downloadServices.Add(downloadService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating download service for {name}", config.Name);
            }
        }
        
        foreach (var downloadService in downloadServices)
        {
            await downloadService.LoginAsync();
        }

        return downloadServices;
    }
}