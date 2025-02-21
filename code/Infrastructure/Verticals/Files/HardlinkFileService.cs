using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

namespace Infrastructure.Verticals.Files;

public class HardlinkFileService : IHardlinkFileService
{
    private readonly ILogger<HardlinkFileService> _logger;
    
    public HardlinkFileService(ILogger<HardlinkFileService> logger)
    {
        _logger = logger;
    }
    
    public ulong GetHardLinkCount(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("file {file} does not exist", filePath);
            return default;
        }
        
        // TODO remove
        _logger.LogDebug("file {file} exists", filePath);
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogDebug("Windows platform detected");
            return GetWindowsHardLinkCount(filePath);
        }

        // TODO remove
        _logger.LogDebug("Unix platform detected");
        return GetUnixHardLinkCount(filePath);
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

    private ulong GetUnixHardLinkCount(string filePath)
    {
        try
        {
            Stat stat = default;
            
            if (stat_file(filePath, ref stat) == 0)
            {
                // TODO remove
                _logger.LogDebug("file {file} has {links} links", filePath, stat.st_nlink);
                return stat.st_nlink;
            }
        }
        catch (Exception exception)
        {
            // TODO log download name?
            _logger.LogError(exception, "failed to stat Unix file {file}", filePath);
        }

        return 0;
    }

    [DllImport("libc", EntryPoint = "stat", SetLastError = true)]
    static extern int stat_file(string path, ref Stat statStruct);

    [StructLayout(LayoutKind.Sequential)]
    struct Stat
    {
        public ulong st_dev;
        public ulong st_ino;   // Inode number
        public ulong st_nlink; // Hard link count
        public uint st_mode;
        public uint st_uid;
        public uint st_gid;
        public ulong st_rdev;
        public long st_size;
        public long st_blksize;
        public long st_blocks;
        public long st_atime;
        public long st_mtime;
        public long st_ctime;
    }
}