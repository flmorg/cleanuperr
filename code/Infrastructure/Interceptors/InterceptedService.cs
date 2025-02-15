namespace Infrastructure.Interceptors;

public class InterceptedService : IInterceptedService
{
    private object? _proxy;

    public object Proxy
    {
        get
        {
            if (_proxy is null)
            {
                throw new Exception("Proxy is not set");
            }

            return _proxy;
        }
        
        set => _proxy = value;
    }
}