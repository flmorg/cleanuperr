using System.Text.Json.Serialization;
using Infrastructure.Health;
using Infrastructure.Logging;
using Infrastructure.Events;
using Infrastructure.Hubs;
using Microsoft.OpenApi.Models;

namespace Executable.DependencyInjection;

public static class ApiDI
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        // Add API-specific services
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        services.AddEndpointsApiExplorer();
        
        // Add SignalR for real-time updates
        services
            .AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });;
        
        // Add health status broadcaster
        services.AddHostedService<HealthStatusBroadcaster>();
        
        // Add logging initializer service
        services.AddHostedService<LoggingInitializer>();
        
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Cleanuparr API",
                Version = "v1",
                Description = "API for managing media downloads and cleanups",
                Contact = new OpenApiContact
                {
                    Name = "Cleanuparr Team"
                }
            });
        });

        return services;
    }

    public static WebApplication ConfigureApi(this WebApplication app)
    {
        app.UseCors("SignalRPolicy");
        app.UseRouting();

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
        app.MapHub<HealthStatusHub>("/api/hubs/health");
        
        // Legacy hubs (for backward compatibility)
        app.MapHub<LogHub>("/api/hubs/logs");
        app.MapHub<EventHub>("/api/hubs/events");
        
        // New unified hub
        app.MapHub<AppHub>("/api/hubs/app");

        return app;
    }
}
