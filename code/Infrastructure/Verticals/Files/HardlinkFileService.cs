using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using Mono.Unix.Native;

namespace Infrastructure.Verticals.Files;

public class HardlinkFileService : IHardlinkFileService
{
    private readonly ILogger<HardlinkFileService> _logger;
    // Track inode counts in the ignored directory (e.g., root directory)
    private readonly ConcurrentDictionary<ulong, int> _inodeCounts = new();
    
    public HardlinkFileService(ILogger<HardlinkFileService> logger)
    {
        _logger = logger;
    }
    
    public ulong GetHardLinkCount(string filePath, bool ignoreRootDir)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("file {file} does not exist", filePath);
            return default;
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogDebug("Windows platform detected");
            return GetWindowsHardLinkCount(filePath);
        }

        return GetUnixHardLinkCount(filePath, ignoreRootDir);
    }

    private uint GetWindowsHardLinkCount(string filePath)
    {
        try
        {
            using SafeFileHandle fileStream = File.OpenHandle(filePath);

            if (GetFileInformationByHandle(fileStream, out var file))
            {
                return file.NumberOfLinks;
            }
        }
        catch (Exception exception)
        {
            // TODO log download name?
            _logger.LogError(exception, "failed to stat Windows file {file}", filePath);
        }

        return default;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(
        SafeFileHandle hFile,
        out BY_HANDLE_FILE_INFORMATION lpFileInformation
    );

    private struct BY_HANDLE_FILE_INFORMATION
    {
        public uint FileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    // Call this first to populate inode counts from the directory you want to ignore
    public void PopulateInodeCounts(string directoryPath)
    {
        try
        {
            // Traverse all files and directories in the ignored path
            foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                AddInodeToCount(file);
            }

            foreach (var dir in Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories))
            {
                AddInodeToCount(dir);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to populate inode counts from {dir}", directoryPath);
        }
    }

    private void AddInodeToCount(string path)
    {
        try
        {
            if (Syscall.stat(path, out Stat stat) == 0)
            {
                _inodeCounts.AddOrUpdate(stat.st_ino, 1, (_, count) => count + 1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Couldn't stat {path} during inode counting", path);
        }
    }

    // Modified GetUnixHardLinkCount with ignore logic
    public ulong GetUnixHardLinkCount(string filePath, bool ignoreRootDir)
    {
        try
        {
            if (Syscall.stat(filePath, out Stat stat) != 0)
                return 0;

            if (!ignoreRootDir)
            {
                // Simple case: Just check if >1 hardlink exists
                return stat.st_nlink > 1 ? stat.st_nlink : 0;
            }

            // Adjusted case: Subtract links from the ignored directory
            int linksInIgnoredDir = _inodeCounts.TryGetValue(stat.st_ino, out int count) 
                ? count 
                : 1; // Default to 1 if not found

            long adjustedCount = (long)stat.st_nlink - linksInIgnoredDir;
            return (ulong)Math.Max(adjustedCount, 0);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to stat file {file}", filePath);
            return 0;
        }
    }
}