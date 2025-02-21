namespace Infrastructure.Verticals.Files;

public interface IHardlinkFileService
{
    void PopulateInodeCounts(string directoryPath);
    long GetHardLinkCount(string filePath, bool ignoreRootDir);
}