using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Mono.Unix.Native;

namespace Infrastructure.Verticals.Files;

public class UnixHardlinkFileService
{
    private readonly ILogger<UnixHardlinkFileService> _logger;
    // Track inode counts in the ignored directory (e.g., root directory)
    private readonly ConcurrentDictionary<ulong, int> _inodeCounts = new();
    
    public UnixHardlinkFileService(ILogger<UnixHardlinkFileService> logger)
    {
        _logger = logger;
    }
    
    public long GetHardlinkCount(string filePath, bool ignoreRootDir)
    {
        try
        {
            if (Syscall.stat(filePath, out Stat stat) != 0)
            {
                _logger.LogDebug("failed to stat file {file}", filePath);
                return -1;
            }

            if (!ignoreRootDir)
            {
                // Simple case: Just check if >1 hardlink exists
                _logger.LogDebug("stat file | hardlinks: {nlink} | {file}", stat.st_nlink, filePath);
                return (long)stat.st_nlink;
            }

            // Adjusted case: Subtract links from the ignored directory
            int linksInIgnoredDir = _inodeCounts.TryGetValue(stat.st_ino, out int count) 
                ? count 
                : 1; // Default to 1 if not found
            
            _logger.LogDebug("stat file | hardlinks: {nlink} | ignored: {ignored} | {file}", stat.st_nlink, linksInIgnoredDir, filePath);

            long adjustedCount = (long)stat.st_nlink - linksInIgnoredDir;
            return Math.Max(adjustedCount, 0);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "failed to stat file {file}", filePath);
            return -1;
        }
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
}