using Executable.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var host = builder.Build();

host.Run();