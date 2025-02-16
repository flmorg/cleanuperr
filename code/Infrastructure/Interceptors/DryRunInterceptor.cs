using System.Reflection;
using Castle.DynamicProxy;
using Common.Attributes;
using Common.Configuration.General;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Interceptors;

public class DryRunAsyncInterceptor : AsyncInterceptorBase
{
    private readonly ILogger<DryRunAsyncInterceptor> _logger;
    private readonly DryRunConfig _config;
    
    public DryRunAsyncInterceptor(ILogger<DryRunAsyncInterceptor> logger, IOptions<DryRunConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }
    
    protected override async Task InterceptAsync(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task> proceed)
    {
        MethodInfo? method = invocation.MethodInvocationTarget ?? invocation.Method;
        if (IsDryRun(method))
        {
            _logger.LogInformation("[DRY RUN] skipping method: {name}", method.Name);
            return;
        }

        await proceed(invocation, proceedInfo);
    }

    protected override async Task<TResult> InterceptAsync<TResult>(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
    {
        MethodInfo? method = invocation.MethodInvocationTarget ?? invocation.Method;
        if (IsDryRun(method))
        {
            _logger.LogInformation("[DRY RUN] skipping method: {name}", method.Name);
            return default!;
        }

        return await proceed(invocation, proceedInfo);
    }

    private bool IsDryRun(MethodInfo method)
    {
        return method.GetCustomAttributes(typeof(DryRunSafeguardAttribute), true).Any() && _config.IsDryRun;
    }
}
