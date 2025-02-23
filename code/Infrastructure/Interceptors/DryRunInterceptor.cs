using System.Reflection;
using Common.Attributes;
using Common.Configuration.General;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Interceptors;

public class DryRunInterceptor : IDryRunInterceptor
{
    private readonly ILogger<DryRunInterceptor> _logger;
    private readonly DryRunConfig _config;
    
    public DryRunInterceptor(ILogger<DryRunInterceptor> logger, IOptions<DryRunConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }
    
    public void Intercept(Action action)
    {
        MethodInfo methodInfo = action.Method;
        
        if (IsDryRun(methodInfo))
        {
            _logger.LogInformation("[DRY RUN] skipping method: {name}", methodInfo.Name);
            return;
        }

        action();
    }
    
    public Task InterceptAsync(Delegate action, params object[] parameters)
    {
        MethodInfo methodInfo = action.Method;
        
        if (IsDryRun(methodInfo))
        {
            _logger.LogInformation("[DRY RUN] skipping method: {name}", methodInfo.Name);
            return Task.CompletedTask;
        }

        object? result = action.DynamicInvoke(parameters);

        if (result is Task task)
        {
            return task;
        }

        return Task.CompletedTask;
    }
    
    public Task<T?> InterceptAsync<T>(Delegate action, params object[] parameters)
    {
        MethodInfo methodInfo = action.Method;
        
        if (IsDryRun(methodInfo))
        {
            _logger.LogInformation("[DRY RUN] skipping method: {name}", methodInfo.Name);
            return Task.FromResult(default(T));
        }

        object? result = action.DynamicInvoke(parameters);

        if (result is Task<T?> task)
        {
            return task;
        }

        return Task.FromResult(default(T));
    }
    
    private bool IsDryRun(MethodInfo method)
    {
        return method.GetCustomAttributes(typeof(DryRunSafeguardAttribute), true).Any() && _config.IsDryRun;
    }
}
