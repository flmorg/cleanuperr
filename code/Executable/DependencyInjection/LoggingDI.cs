using Common.Configuration.Logging;
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
        const string consoleOutputTemplate = "[{@t:yyyy-MM-dd HH:mm:ss.fff} {@l:u3}]{#if JobName is not null} {Concat('[',JobName,']'),PAD}{#end} {@m}\n{@x}";
        const string fileOutputTemplate = "{@t:yyyy-MM-dd HH:mm:ss.fff zzz} [{@l:u3}]{#if JobName is not null} {Concat('[',JobName,']'),PAD}{#end} {@m:lj}\n{@x}";
        LogEventLevel level = LogEventLevel.Information;
        List<string> jobNames = [nameof(ContentBlocker), nameof(QueueCleaner)];
        int padding = jobNames.Max(x => x.Length) + 2;

        if (config is not null)
        {
            level = config.LogLevel;

            if (config.File?.Enabled is true)
            {
                logConfig.WriteTo.File(
                    path: Path.Combine(config.File.Path, "cleanuperr-.txt"),
                    formatter: new ExpressionTemplate(fileOutputTemplate.Replace("PAD", padding.ToString())),
                    fileSizeLimitBytes: 10L * 1024 * 1024,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true
                );
            }
        }

        Log.Logger = logConfig
            .MinimumLevel.Is(level)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Warning)
            .MinimumLevel.Override("Quartz", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Error)
            .WriteTo.Console(new ExpressionTemplate(consoleOutputTemplate.Replace("PAD", padding.ToString())))
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", "cleanuperr")
            .CreateLogger();
        
        return builder
            .ClearProviders()
            .AddSerilog();
    }
}