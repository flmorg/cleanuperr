using System.Reflection;
using Executable.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Logging.AddLogging(builder.Configuration);

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

var version = Assembly.GetExecutingAssembly().GetName().Version;

logger.LogInformation(
    version is null
        ? "cleanuperr version not detected"
        : $"cleanuperr v{version.Major}.{version.Minor}.{version.Build}"
);

host.Run();