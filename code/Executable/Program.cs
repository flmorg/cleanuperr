using Executable;
using Executable.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Logging.AddLogging(builder.Configuration);

var host = builder.Build();
host.Init();

host.Run();