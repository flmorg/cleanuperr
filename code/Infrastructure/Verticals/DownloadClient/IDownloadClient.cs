namespace Infrastructure.Verticals.DownloadClient;

public interface IDownloadClient : IDisposable
{
    public Task LoginAsync();

    public Task<bool> ShouldRemoveFromArrQueue(string hash);

    public Task BlockUnwantedFiles(string hash);
}