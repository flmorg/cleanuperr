using Common.Configuration.Arr;
using Common.Configuration.DownloadClient;
using Infrastructure.Verticals.Arr;
using Infrastructure.Verticals.DownloadClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Executable.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly ILogger<StatusController> _logger;
    private readonly IOptionsMonitor<DownloadClientConfig> _downloadClientConfig;
    private readonly IOptionsMonitor<SonarrConfig> _sonarrConfig;
    private readonly IOptionsMonitor<RadarrConfig> _radarrConfig;
    private readonly IOptionsMonitor<LidarrConfig> _lidarrConfig;
    private readonly DownloadServiceFactory _downloadServiceFactory;
    private readonly ArrClientFactory _arrClientFactory;

    public StatusController(
        ILogger<StatusController> logger,
        IOptionsMonitor<DownloadClientConfig> downloadClientConfig,
        IOptionsMonitor<SonarrConfig> sonarrConfig,
        IOptionsMonitor<RadarrConfig> radarrConfig,
        IOptionsMonitor<LidarrConfig> lidarrConfig,
        DownloadServiceFactory downloadServiceFactory,
        ArrClientFactory arrClientFactory)
    {
        _logger = logger;
        _downloadClientConfig = downloadClientConfig;
        _sonarrConfig = sonarrConfig;
        _radarrConfig = radarrConfig;
        _lidarrConfig = lidarrConfig;
        _downloadServiceFactory = downloadServiceFactory;
        _arrClientFactory = arrClientFactory;
    }

    [HttpGet]
    public IActionResult GetSystemStatus()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            
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
                    Type = _downloadClientConfig.CurrentValue.DownloadClient.ToString(),
                    IsConfigured = _downloadClientConfig.CurrentValue.DownloadClient != Common.Enums.DownloadClient.None &&
                                  _downloadClientConfig.CurrentValue.DownloadClient != Common.Enums.DownloadClient.Disabled
                },
                MediaManagers = new
                {
                    Sonarr = new
                    {
                        IsEnabled = _sonarrConfig.CurrentValue.Enabled,
                        InstanceCount = _sonarrConfig.CurrentValue.Instances?.Count ?? 0
                    },
                    Radarr = new
                    {
                        IsEnabled = _radarrConfig.CurrentValue.Enabled,
                        InstanceCount = _radarrConfig.CurrentValue.Instances?.Count ?? 0
                    },
                    Lidarr = new
                    {
                        IsEnabled = _lidarrConfig.CurrentValue.Enabled,
                        InstanceCount = _lidarrConfig.CurrentValue.Instances?.Count ?? 0
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
            if (_downloadClientConfig.CurrentValue.DownloadClient == Common.Enums.DownloadClient.None ||
                _downloadClientConfig.CurrentValue.DownloadClient == Common.Enums.DownloadClient.Disabled)
            {
                return NotFound("No download client is configured");
            }

            var downloadService = _downloadServiceFactory.CreateDownloadClient();
            
            try
            {
                await downloadService.LoginAsync();
                
                // Basic status info that should be safe for any download client
                var status = new
                {
                    IsConnected = true,
                    ClientType = _downloadClientConfig.CurrentValue.DownloadClient.ToString(),
                    Message = "Successfully connected to download client"
                };
                
                return Ok(status);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    IsConnected = false,
                    ClientType = _downloadClientConfig.CurrentValue.DownloadClient.ToString(),
                    Message = $"Failed to connect to download client: {ex.Message}"
                });
            }
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

            // Check Sonarr instances
            if (_sonarrConfig.CurrentValue.Enabled && _sonarrConfig.CurrentValue.Instances?.Count > 0)
            {
                var sonarrStatus = new List<object>();
                
                foreach (var instance in _sonarrConfig.CurrentValue.Instances)
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
            if (_radarrConfig.CurrentValue.Enabled && _radarrConfig.CurrentValue.Instances?.Count > 0)
            {
                var radarrStatus = new List<object>();
                
                foreach (var instance in _radarrConfig.CurrentValue.Instances)
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
            if (_lidarrConfig.CurrentValue.Enabled && _lidarrConfig.CurrentValue.Instances?.Count > 0)
            {
                var lidarrStatus = new List<object>();
                
                foreach (var instance in _lidarrConfig.CurrentValue.Instances)
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
