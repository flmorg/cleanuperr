using System.Text.Json.Serialization;
using Cleanuparr.Api;
using Cleanuparr.Api.DependencyInjection;
using Cleanuparr.Infrastructure.Logging;
using Cleanuparr.Shared.Helpers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile(Path.Combine(ConfigurationPathProvider.GetConfigPath(), "cleanuparr.json"), optional: true, reloadOnChange: true);

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Configure JSON options to serialize enums as strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Add services to the container
builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApiServices();

// Add CORS before SignalR
builder.Services.AddCors(options => 
{
    options.AddPolicy("Any", policy => 
    {
        policy
            // https://github.com/dotnet/aspnetcore/issues/4457#issuecomment-465669576
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR auth
    });
});

// Register services needed for logging first
builder.Services
    .AddTransient<LoggingConfigManager>()
    .AddSingleton<SignalRLogSink>();

// Add logging with proper service provider
builder.Logging.AddLogging();

var app = builder.Build();

// Configure BASE_PATH immediately after app build and before any other configuration
string? basePath = app.Configuration.GetValue<string>("BASE_PATH");
ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();

if (basePath is not null)
{
    // Validate the base path
    var validationResult = BasePathValidator.Validate(basePath);
    if (!validationResult.IsValid)
    {
        logger.LogError("Invalid BASE_PATH configuration: {ErrorMessage}", validationResult.ErrorMessage);
        return;
    }

    // Normalize the base path
    basePath = BasePathValidator.Normalize(basePath);
    
    if (!string.IsNullOrEmpty(basePath))
    {
        app.Use(async (context, next) =>
        {
            if (!string.IsNullOrEmpty(basePath) && !context.Request.Path.StartsWithSegments(basePath, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next();
        });
        app.UsePathBase(basePath);
    }
    else
    {
        logger.LogInformation("No base path configured - serving from root");
    }
}

logger.LogInformation("Server configuration: PORT={port}, BASE_PATH={basePath}", app.Configuration.GetValue<string>("HTTP_PORTS"), basePath ?? "/");

// Initialize the host
await app.Init();

// Get LoggingConfigManager (will be created if not already registered)
var configManager = app.Services.GetRequiredService<LoggingConfigManager>();
        
// Get the dynamic level switch for controlling log levels
var levelSwitch = configManager.GetLevelSwitch();
            
// Get the SignalRLogSink instance
var signalRSink = app.Services.GetRequiredService<SignalRLogSink>();

var logConfig = LoggingDI.GetDefaultLoggerConfiguration();
logConfig.MinimumLevel.ControlledBy(levelSwitch);
        
// Add to Serilog pipeline
logConfig.WriteTo.Sink(signalRSink);

Log.Logger = logConfig.CreateLogger();

app.ConfigureApi();

await app.RunAsync();