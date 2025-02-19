namespace Infrastructure.Verticals.Files;

public interface IHardlinkFileService
{
    uint GetHardLinkCount(string filePath);
}