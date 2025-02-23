namespace Infrastructure.Interceptors;

public interface IDryRunInterceptor
{
    void Intercept(Action action);

    Task InterceptAsync(Func<Task> action);
}