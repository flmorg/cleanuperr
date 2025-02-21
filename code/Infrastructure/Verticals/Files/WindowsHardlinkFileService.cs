using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

namespace Infrastructure.Verticals.Files;

public class WindowsHardlinkFileService
{
    private readonly ILogger<WindowsHardlinkFileService> _logger;

    public WindowsHardlinkFileService(ILogger<WindowsHardlinkFileService> logger)
    {
        _logger = logger;
    }
    
    public long GetWindowsHardLinkCount(string filePath)
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

        return -1;
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
}