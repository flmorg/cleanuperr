namespace Infrastructure.Interceptors;

public interface IInterceptedService
{
    public object Proxy { get; set; }
}