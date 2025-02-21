using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.Files;

public class HardlinkFileService : IHardlinkFileService
{
    private readonly ILogger<HardlinkFileService> _logger;
    private readonly UnixHardlinkFileService _unixHardlinkFileService;
    private readonly WindowsHardlinkFileService _windowsHardlinkFileService;

    public HardlinkFileService(
        ILogger<HardlinkFileService> logger,
        UnixHardlinkFileService unixHardlinkFileService,
        WindowsHardlinkFileService windowsHardlinkFileService
    )
    {
        _logger = logger;
        _unixHardlinkFileService = unixHardlinkFileService;
        _windowsHardlinkFileService = windowsHardlinkFileService;
    }

    public void PopulateInodeCounts(string directoryPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // TODO
            return;
        }
        
        _unixHardlinkFileService.PopulateInodeCounts(directoryPath);
    }

    public long GetHardLinkCount(string filePath, bool ignoreRootDir)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("file {file} does not exist", filePath);
            return -1;
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return _windowsHardlinkFileService.GetWindowsHardLinkCount(filePath);
        }

        return _unixHardlinkFileService.GetHardlinkCount(filePath, ignoreRootDir);
    }
}