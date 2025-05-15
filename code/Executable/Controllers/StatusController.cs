using Common.Configuration.Arr;
using Common.Configuration.DownloadClient;
using Infrastructure.Configuration;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.DownloadClient;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Executable.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly ILogger<StatusController> _logger;
    private readonly IConfigurationManager _configManager;
    private readonly DownloadServiceFactory _downloadServiceFactory;
    private readonly ArrClientFactory _arrClientFactory;

    public StatusController(
        ILogger<StatusController> logger,
        IConfigurationManager configManager,
        DownloadServiceFactory downloadServiceFactory,
        ArrClientFactory arrClientFactory)
    {
        _logger = logger;
        _configManager = configManager;
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
            var downloadClientConfig = await _configManager.GetDownloadClientConfigAsync();
            var sonarrConfig = await _configManager.GetSonarrConfigAsync();
            var radarrConfig = await _configManager.GetRadarrConfigAsync();
            var lidarrConfig = await _configManager.GetLidarrConfigAsync();
            
            // Default if configs are null
            downloadClientConfig ??= new DownloadClientConfig();
            sonarrConfig ??= new SonarrConfig();
            radarrConfig ??= new RadarrConfig();
            lidarrConfig ??= new LidarrConfig();
            
            var status = new
            {
                Application = new
                {
                    Version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown",
                    StartTime = process.StartTime,
                    UpTime = DateTime.Now - process.StartTime,
                    MemoryUsageMB = Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 2),
                    ProcessorTime = process.TotalProcessorTime
                },
                DownloadClient = new
                {
                    LegacyType = downloadClientConfig.DownloadClient.ToString(),
                    ConfiguredClientCount = downloadClientConfig.Clients.Count,
                    EnabledClientCount = downloadClientConfig.Clients.Count(c => c.Enabled),
                    IsConfigured = downloadClientConfig.DownloadClient != Common.Enums.DownloadClient.None &&
                                  downloadClientConfig.DownloadClient != Common.Enums.DownloadClient.Disabled ||
                                  downloadClientConfig.Clients.Any(c => c.Enabled)
                },
                MediaManagers = new
                {
                    Sonarr = new
                    {
                        IsEnabled = sonarrConfig.Enabled,
                        InstanceCount = sonarrConfig.Instances?.Count ?? 0
                    },
                    Radarr = new
                    {
                        IsEnabled = radarrConfig.Enabled,
                        InstanceCount = radarrConfig.Instances?.Count ?? 0
                    },
                    Lidarr = new
                    {
                        IsEnabled = lidarrConfig.Enabled,
                        InstanceCount = lidarrConfig.Instances?.Count ?? 0
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
            var downloadClientConfig = await _configManager.GetDownloadClientConfigAsync();
            if (downloadClientConfig == null)
            {
                return NotFound("Download client configuration not found");
            }
            
            var result = new Dictionary<string, object>();
            
            // Check for configured clients
            if (downloadClientConfig.Clients.Count > 0)
            {
                var clientsStatus = new List<object>();
                foreach (var client in downloadClientConfig.Clients)
                {
                    try
                    {
                        clientsStatus.Add(new
                        {
                            Id = client.Id,
                            Name = client.Name,
                            Type = client.Type.ToString(),
                            Host = client.Host,
                            Port = client.Port,
                            Enabled = client.Enabled,
                            IsConnected = client.Enabled, // We can't check connection status without implementing test methods
                            Status = client.Enabled ? "Enabled" : "Disabled"
                        });
                    }
                    catch (Exception ex)
                    {
                        clientsStatus.Add(new
                        {
                            Id = client.Id,
                            Name = client.Name,
                            Type = client.Type.ToString(),
                            Host = client.Host,
                            Port = client.Port,
                            Enabled = client.Enabled,
                            IsConnected = false,
                            Status = $"Error: {ex.Message}"
                        });
                    }
                }
                
                result["Clients"] = clientsStatus;
            }
            else if (downloadClientConfig.DownloadClient != Common.Enums.DownloadClient.None &&
                     downloadClientConfig.DownloadClient != Common.Enums.DownloadClient.Disabled)
            {
                // Legacy configuration
                result["LegacyClient"] = new
                {
                    Type = downloadClientConfig.DownloadClient.ToString(),
                    Host = downloadClientConfig.Host,
                    Port = downloadClientConfig.Port,
                    IsConnected = true // We can't check without implementing test methods in clients
                };
            }
            else
            {
                result["Status"] = "No download clients configured";
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving download client status");
            return StatusCode(500, "An error occurred while retrieving download client status");
        }
    }

    [HttpGet("media-managers")]
    public async Task<IActionResult> GetMediaManagersStatus()
    {
        try
        {
            var status = new Dictionary<string, object>();
            
            // Get configurations
            var sonarrConfig = await _configManager.GetSonarrConfigAsync();
            var radarrConfig = await _configManager.GetRadarrConfigAsync();
            var lidarrConfig = await _configManager.GetLidarrConfigAsync();

            // Check Sonarr instances
            if (sonarrConfig?.Enabled == true && sonarrConfig.Instances?.Count > 0)
            {
                var sonarrStatus = new List<object>();
                
                foreach (var instance in sonarrConfig.Instances)
                {
                    try
                    {
                        var sonarrClient = _arrClientFactory.GetClient(Domain.Enums.InstanceType.Sonarr);
                        await sonarrClient.TestConnectionAsync(instance);
                        
                        sonarrStatus.Add(new
                        {
                            Name = instance.Name,
                            Url = instance.Url,
                            IsConnected = true,
                            Message = "Successfully connected"
                        });
                    }
                    catch (Exception ex)
                    {
                        sonarrStatus.Add(new
                        {
                            Name = instance.Name,
                            Url = instance.Url,
                            IsConnected = false,
                            Message = $"Connection failed: {ex.Message}"
                        });
                    }
                }

                status["Sonarr"] = sonarrStatus;
            }

            // Check Radarr instances
            if (radarrConfig?.Enabled == true && radarrConfig.Instances?.Count > 0)
            {
                var radarrStatus = new List<object>();
                
                foreach (var instance in radarrConfig.Instances)
                {
                    try
                    {
                        var radarrClient = _arrClientFactory.GetClient(Domain.Enums.InstanceType.Radarr);
                        await radarrClient.TestConnectionAsync(instance);
                        
                        radarrStatus.Add(new
                        {
                            Name = instance.Name,
                            Url = instance.Url,
                            IsConnected = true,
                            Message = "Successfully connected"
                        });
                    }
                    catch (Exception ex)
                    {
                        radarrStatus.Add(new
                        {
                            Name = instance.Name,
                            Url = instance.Url,
                            IsConnected = false,
                            Message = $"Connection failed: {ex.Message}"
                        });
                    }
                }

                status["Radarr"] = radarrStatus;
            }

            // Check Lidarr instances
            if (lidarrConfig?.Enabled == true && lidarrConfig.Instances?.Count > 0)
            {
                var lidarrStatus = new List<object>();
                
                foreach (var instance in lidarrConfig.Instances)
                {
                    try
                    {
                        var lidarrClient = _arrClientFactory.GetClient(Domain.Enums.InstanceType.Lidarr);
                        await lidarrClient.TestConnectionAsync(instance);
                        
                        lidarrStatus.Add(new
                        {
                            Name = instance.Name,
                            Url = instance.Url,
                            IsConnected = true,
                            Message = "Successfully connected"
                        });
                    }
                    catch (Exception ex)
                    {
                        lidarrStatus.Add(new
                        {
                            Name = instance.Name,
                            Url = instance.Url,
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
