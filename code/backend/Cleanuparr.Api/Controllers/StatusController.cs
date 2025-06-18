using System.Diagnostics;
using Cleanuparr.Domain.Enums;
using Cleanuparr.Infrastructure.Features.Arr;
using Cleanuparr.Infrastructure.Features.DownloadClient;
using Cleanuparr.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleanuparr.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly ILogger<StatusController> _logger;
    private readonly DataContext _dataContext;
    private readonly DownloadServiceFactory _downloadServiceFactory;
    private readonly ArrClientFactory _arrClientFactory;

    public StatusController(
        ILogger<StatusController> logger,
        DataContext dataContext,
        DownloadServiceFactory downloadServiceFactory,
        ArrClientFactory arrClientFactory)
    {
        _logger = logger;
        _dataContext = dataContext;
        _downloadServiceFactory = downloadServiceFactory;
        _arrClientFactory = arrClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetSystemStatus()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            
            // Get configuration
            var downloadClients = await _dataContext.DownloadClients
                .AsNoTracking()
                .ToListAsync();
            var sonarrConfig = await _dataContext.ArrConfigs
                .Include(x => x.Instances)
                .AsNoTracking()
                .FirstAsync(x => x.Type == InstanceType.Sonarr);
            var radarrConfig = await _dataContext.ArrConfigs
                .Include(x => x.Instances)
                .AsNoTracking()
                .FirstAsync(x => x.Type == InstanceType.Radarr);
            var lidarrConfig = await _dataContext.ArrConfigs
                .Include(x => x.Instances)
                .AsNoTracking()
                .FirstAsync(x => x.Type == InstanceType.Lidarr);
            
            var status = new
            {
                Application = new
                {
                    Version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown",
                    process.StartTime,
                    UpTime = DateTime.Now - process.StartTime,
                    MemoryUsageMB = Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 2),
                    ProcessorTime = process.TotalProcessorTime
                },
                DownloadClient = new
                {
                    // TODO
                },
                MediaManagers = new
                {
                    Sonarr = new
                    {
                        IsEnabled = sonarrConfig.Enabled,
                        InstanceCount = sonarrConfig.Instances.Count
                    },
                    Radarr = new
                    {
                        IsEnabled = radarrConfig.Enabled,
                        InstanceCount = radarrConfig.Instances.Count
                    },
                    Lidarr = new
                    {
                        IsEnabled = lidarrConfig.Enabled,
                        InstanceCount = lidarrConfig.Instances.Count
                    }
                }
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system status");
            return StatusCode(500, "An error occurred while retrieving system status");
        }
    }

    [HttpGet("download-client")]
    public async Task<IActionResult> GetDownloadClientStatus()
    {
        try
        {
            var downloadClients = await _dataContext.DownloadClients
                .AsNoTracking()
                .ToListAsync();
            var result = new Dictionary<string, object>();
            
            // Check for configured clients
            if (downloadClients.Count > 0)
            {
                var clientsStatus = new List<object>();
                foreach (var client in downloadClients)
                {
                    clientsStatus.Add(new
                    {
                        client.Id,
                        client.Name,
                        Type = client.TypeName,
                        client.Host,
                        client.Enabled,
                        IsConnected = client.Enabled, // We can't check connection status without implementing test methods
                    });
                }
                
                result["Clients"] = clientsStatus;
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving download client status");
            return StatusCode(500, "An error occurred while retrieving download client status");
        }
    }

    [HttpGet("arrs")]
    public async Task<IActionResult> GetMediaManagersStatus()
    {
        try
        {
            var status = new Dictionary<string, object>();
            
            // Get configurations
            var sonarrConfig = await _dataContext.ArrConfigs
                .Include(x => x.Instances)
                .AsNoTracking()
                .FirstAsync(x => x.Type == InstanceType.Sonarr);
            var radarrConfig = await _dataContext.ArrConfigs
                .Include(x => x.Instances)
                .AsNoTracking()
                .FirstAsync(x => x.Type == InstanceType.Radarr);
            var lidarrConfig = await _dataContext.ArrConfigs
                .Include(x => x.Instances)
                .AsNoTracking()
                .FirstAsync(x => x.Type == InstanceType.Lidarr);

            // Check Sonarr instances
            if (sonarrConfig is { Enabled: true, Instances.Count: > 0 })
            {
                var sonarrStatus = new List<object>();
                
                foreach (var instance in sonarrConfig.Instances)
                {
                    try
                    {
                        var sonarrClient = _arrClientFactory.GetClient(InstanceType.Sonarr);
                        await sonarrClient.TestConnectionAsync(instance);
                        
                        sonarrStatus.Add(new
                        {
                            instance.Name,
                            instance.Url,
                            IsConnected = true,
                            Message = "Successfully connected"
                        });
                    }
                    catch (Exception ex)
                    {
                        sonarrStatus.Add(new
                        {
                            instance.Name,
                            instance.Url,
                            IsConnected = false,
                            Message = $"Connection failed: {ex.Message}"
                        });
                    }
                }

                status["Sonarr"] = sonarrStatus;
            }

            // Check Radarr instances
            if (radarrConfig is { Enabled: true, Instances.Count: > 0 })
            {
                var radarrStatus = new List<object>();
                
                foreach (var instance in radarrConfig.Instances)
                {
                    try
                    {
                        var radarrClient = _arrClientFactory.GetClient(InstanceType.Radarr);
                        await radarrClient.TestConnectionAsync(instance);
                        
                        radarrStatus.Add(new
                        {
                            instance.Name,
                            instance.Url,
                            IsConnected = true,
                            Message = "Successfully connected"
                        });
                    }
                    catch (Exception ex)
                    {
                        radarrStatus.Add(new
                        {
                            instance.Name,
                            instance.Url,
                            IsConnected = false,
                            Message = $"Connection failed: {ex.Message}"
                        });
                    }
                }

                status["Radarr"] = radarrStatus;
            }

            // Check Lidarr instances
            if (lidarrConfig is { Enabled: true, Instances.Count: > 0 })
            {
                var lidarrStatus = new List<object>();
                
                foreach (var instance in lidarrConfig.Instances)
                {
                    try
                    {
                        var lidarrClient = _arrClientFactory.GetClient(InstanceType.Lidarr);
                        await lidarrClient.TestConnectionAsync(instance);
                        
                        lidarrStatus.Add(new
                        {
                            instance.Name,
                            instance.Url,
                            IsConnected = true,
                            Message = "Successfully connected"
                        });
                    }
                    catch (Exception ex)
                    {
                        lidarrStatus.Add(new
                        {
                            instance.Name,
                            instance.Url,
                            IsConnected = false,
                            Message = $"Connection failed: {ex.Message}"
                        });
                    }
                }

                status["Lidarr"] = lidarrStatus;
            }

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving media managers status");
            return StatusCode(500, "An error occurred while retrieving media managers status");
        }
    }
}
