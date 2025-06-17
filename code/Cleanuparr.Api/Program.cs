// using Infrastructure.Logging;
// using Serilog;
using System.Text.Json.Serialization;
using Cleanuparr.Api;
using Cleanuparr.Api.DependencyInjection;
using Cleanuparr.Infrastructure.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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
    options.AddPolicy("SignalRPolicy", policy => 
    {
        policy.WithOrigins("http://localhost:4200") // Your Angular URL
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

// Configure the HTTP request pipeline
app.ConfigureApi();

await app.RunAsync();