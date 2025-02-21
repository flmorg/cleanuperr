namespace Infrastructure.Verticals.Files;

public interface IHardlinkFileService
{
    ulong GetHardLinkCount(string filePath);
}