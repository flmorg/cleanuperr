namespace Infrastructure.Verticals.Jobs;

public interface IHandler
{
    Task ExecuteAsync();
}