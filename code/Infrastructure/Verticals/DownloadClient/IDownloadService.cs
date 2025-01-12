namespace Infrastructure.Verticals.DownloadClient;

public interface IDownloadService : IDisposable
{
    public Task LoginAsync();

    public Task<RemoveResult> ShouldRemoveFromArrQueueAsync(string hash);

    public Task BlockUnwantedFilesAsync(string hash);
}