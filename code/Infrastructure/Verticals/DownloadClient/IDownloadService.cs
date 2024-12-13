namespace Infrastructure.Verticals.DownloadClient;

public interface IDownloadService : IDisposable
{
    public Task LoginAsync();

    public Task<bool> ShouldRemoveFromArrQueueAsync(string hash, ushort maxStrikes);

    public Task BlockUnwantedFilesAsync(string hash);
}