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
    
    public uint GetHardLinkCount(string filePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWindowsHardLinkCount(filePath);
        }

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

    private uint GetUnixHardLinkCount(string filePath)
    {
        try
        {
            if (stat64(filePath, out Stat stat) == 0)
            {
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

    [DllImport("libc", SetLastError = true)]
    private static extern int stat64(string path, out Stat stat);

    [StructLayout(LayoutKind.Sequential)]
    private struct Stat
    {
        public uint st_dev;
        public ulong st_ino;
        public uint st_mode;
        public uint st_nlink; // Hard link count
        public uint st_uid;
        public uint st_gid;
        public uint st_rdev;
        public long st_size;
    }
}