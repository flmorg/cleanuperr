using Executable;
using Executable.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApiServices();

builder.Logging.AddLogging(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
app.ConfigureApi();

// Initialize the host
await app.Init();

await app.RunAsync();