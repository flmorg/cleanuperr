using Executable;
using Executable.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApiServices();

// Register services needed for logging first
builder.Services.AddSingleton<Infrastructure.Logging.LoggingConfigManager>();

// Add logging with proper service provider
var serviceProvider = builder.Services.BuildServiceProvider();
await builder.Logging.AddLogging(serviceProvider);

var app = builder.Build();

// Configure the HTTP request pipeline
app.ConfigureApi();

// Initialize the host
await app.Init();

await app.RunAsync();