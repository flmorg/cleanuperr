using Cleanuparr.Api.Jobs;
using Quartz;

namespace Cleanuparr.Api.DependencyInjection;

public static class QuartzDI
{
    public static IServiceCollection AddQuartzServices(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddQuartz()
            .AddQuartzHostedService(opt =>
            {
                opt.WaitForJobsToComplete = true;
            })
            // Register BackgroundJobManager as a hosted service
            .AddSingleton<BackgroundJobManager>()
            .AddHostedService(provider => provider.GetRequiredService<BackgroundJobManager>());

    // Jobs are now managed by BackgroundJobManager
}