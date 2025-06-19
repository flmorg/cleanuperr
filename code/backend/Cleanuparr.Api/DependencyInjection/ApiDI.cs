using System.Text.Json.Serialization;
using Cleanuparr.Api.Middleware;
using Cleanuparr.Infrastructure.Health;
using Cleanuparr.Infrastructure.Hubs;
using Cleanuparr.Infrastructure.Logging;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi.Models;

namespace Cleanuparr.Api.DependencyInjection;

public static class ApiDI
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });
        
        // Add API-specific services
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
        services.AddEndpointsApiExplorer();
        
        // Add SignalR for real-time updates
        services
            .AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        
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
        ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
        
        // Enable compression
        app.UseResponseCompression();
        
        // Serve static files with caching
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                // Cache static assets for 30 days
                // if (ctx.File.Name.EndsWith(".js") || ctx.File.Name.EndsWith(".css"))
                // {
                //     ctx.Context.Response.Headers.CacheControl = "public,max-age=2592000";
                // }
            }
        });
        
        // Add the global exception handling middleware first
        app.UseMiddleware<ExceptionMiddleware>();
        
        app.UseCors("Any");
        app.UseRouting();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("v1/swagger.json", "Cleanuparr API v1");
                options.RoutePrefix = "swagger";
                options.DocumentTitle = "Cleanuparr API Documentation";
            });
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        
        // SPA fallback - must be last
        app.MapFallbackToFile("index.html");
        
        // Map SignalR hubs
        app.MapHub<HealthStatusHub>("/api/hubs/health");
        app.MapHub<AppHub>("/api/hubs/app");

        return app;
    }
}
