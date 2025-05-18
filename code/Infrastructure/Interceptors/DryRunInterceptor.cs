using System.Reflection;
using Common.Configuration.General;
using Microsoft.Extensions.Logging;
using Infrastructure.Configuration;

namespace Infrastructure.Interceptors;

public class DryRunInterceptor : IDryRunInterceptor
{
    private readonly ILogger<DryRunInterceptor> _logger;
    private readonly GeneralConfig _config;
    
    public DryRunInterceptor(ILogger<DryRunInterceptor> logger, IConfigManager configManager)
    {
        _logger = logger;
        _config = configManager.GetConfiguration<GeneralConfig>();
    }
    
    public void Intercept(Action action)
    {
        MethodInfo methodInfo = action.Method;
        
        if (_config.DryRun)
        {
            _logger.LogInformation("[DRY RUN] skipping method: {name}", methodInfo.Name);
            return;
        }

        action();
    }
    
    public Task InterceptAsync(Delegate action, params object[] parameters)
    {
        MethodInfo methodInfo = action.Method;
        
        if (_config.DryRun)
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
        
        if (_config.DryRun)
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
}
