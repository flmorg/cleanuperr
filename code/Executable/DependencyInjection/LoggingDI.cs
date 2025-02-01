using Common.Configuration.Logging;
using Domain.Enums;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.QueueCleaner;
using Serilog;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace Executable.DependencyInjection;

public static class LoggingDI
{
    public static ILoggingBuilder AddLogging(this ILoggingBuilder builder, IConfiguration configuration)
    {
        LoggingConfig? config = configuration.GetSection(LoggingConfig.SectionName).Get<LoggingConfig>();

        if (!string.IsNullOrEmpty(config?.File?.Path) && !Directory.Exists(config.File.Path))
        {
            try
            {
                Directory.CreateDirectory(config.File.Path);
            }
            catch (Exception exception)
            {
                throw new Exception($"log file path is not a valid directory | {config.File.Path}", exception);
            }
        }
        
        LoggerConfiguration logConfig = new();
        const string jobNameTemplate = "{#if JobName is not null} {Concat('[',JobName,']'),JOB_PAD}{#end}";
        const string instanceNameTemplate = "{#if InstanceName is not null} {Concat('[',InstanceName,']'),ARR_PAD}{#end}";
        const string consoleOutputTemplate = $"[{{@t:yyyy-MM-dd HH:mm:ss.fff}} {{@l:u3}}]{jobNameTemplate}{instanceNameTemplate} {{@m}}\n{{@x}}";
        const string fileOutputTemplate = $"{{@t:yyyy-MM-dd HH:mm:ss.fff zzz}} [{{@l:u3}}]{jobNameTemplate}{instanceNameTemplate} {{@m:lj}}\n{{@x}}";
        LogEventLevel level = LogEventLevel.Information;
        List<string> names = [nameof(ContentBlocker), nameof(QueueCleaner)];
        int jobPadding = names.Max(x => x.Length) + 2;
        names = [InstanceType.Sonarr.ToString(), InstanceType.Radarr.ToString(), InstanceType.Lidarr.ToString()];
        int arrPadding = names.Max(x => x.Length) + 2;

        string consoleTemplate = consoleOutputTemplate
            .Replace("JOB_PAD", jobPadding.ToString())
            .Replace("ARR_PAD", arrPadding.ToString());
        string fileTemplate = fileOutputTemplate
            .Replace("JOB_PAD", jobPadding.ToString())
            .Replace("ARR_PAD", arrPadding.ToString());

        if (config is not null)
        {
            level = config.LogLevel;

            if (config.File?.Enabled is true)
            {
                logConfig.WriteTo.File(
                    path: Path.Combine(config.File.Path, "cleanuperr-.txt"),
                    formatter: new ExpressionTemplate(fileTemplate),
                    fileSizeLimitBytes: 10L * 1024 * 1024,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true
                );
            }
        }

        Log.Logger = logConfig
            .MinimumLevel.Is(level)
            .MinimumLevel.Override("MassTransit", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Warning)
            .MinimumLevel.Override("Quartz", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Error)
            .WriteTo.Console(new ExpressionTemplate(consoleTemplate))
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", "cleanuperr")
            .CreateLogger();
        
        return builder
            .ClearProviders()
            .AddSerilog();
    }
}