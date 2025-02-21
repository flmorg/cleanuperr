namespace Infrastructure.Verticals.Files;

public interface IHardlinkFileService
{
    void PopulateInodeCounts(string directoryPath);
    ulong GetHardLinkCount(string filePath, bool ignoreRootDir);
}