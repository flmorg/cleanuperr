using Infrastructure.Health;
using Infrastructure.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Executable.DependencyInjection;

public static class ApiDI
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        // Add API-specific services
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        
        // Add SignalR for real-time updates
        services.AddSignalR();
        
        // Add health status broadcaster
        services.AddHostedService<HealthStatusBroadcaster>();
        
        // Add logging initializer service
        services.AddHostedService<LoggingInitializer>();
        
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Cleanuperr API",
                Version = "v1",
                Description = "API for managing media downloads and cleanups",
                Contact = new OpenApiContact
                {
                    Name = "Cleanuperr Team"
                }
            });
        });

        return services;
    }

    public static WebApplication ConfigureApi(this WebApplication app)
    {
        // Configure middleware pipeline for API
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Cleanuperr API v1");
                options.RoutePrefix = "swagger";
                options.DocumentTitle = "Cleanuperr API Documentation";
            });
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        
        // Map SignalR hubs
        app.MapHub<HealthStatusHub>("/hubs/health");
        app.MapHub<LogHub>("/hubs/logs");

        return app;
    }
}
