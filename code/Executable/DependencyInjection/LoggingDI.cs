using Common.Configuration.Logging;
using Domain.Enums;
using Infrastructure.Configuration;
using Infrastructure.Logging;
using Infrastructure.Verticals.ContentBlocker;
using Infrastructure.Verticals.DownloadCleaner;
using Infrastructure.Verticals.QueueCleaner;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace Executable.DependencyInjection;

public static class LoggingDI
{
    public static ILoggingBuilder AddLogging(this ILoggingBuilder builder, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        // Get the logging configuration
        LoggingConfig? config = configuration.GetSection(LoggingConfig.SectionName).Get<LoggingConfig>();

        // Get the configuration path provider
        var pathProvider = serviceProvider.GetRequiredService<ConfigurationPathProvider>();

        // Create the logs directory
        string logsPath = Path.Combine(pathProvider.GetConfigPath(), "logs");
        if (!Directory.Exists(logsPath))
        {
            try
            {
                Directory.CreateDirectory(logsPath);
            }
            catch (Exception exception)
            {
                throw new Exception($"Failed to create log directory | {logsPath}", exception);
            }
        }

        LoggerConfiguration logConfig = new();
        const string categoryTemplate = "{#if Category is not null} {Concat('[',Category,']'),CAT_PAD}{#end}";
        const string jobNameTemplate = "{#if JobName is not null} {Concat('[',JobName,']'),JOB_PAD}{#end}";
        const string instanceNameTemplate = "{#if InstanceName is not null} {Concat('[',InstanceName,']'),ARR_PAD}{#end}";

        const string consoleOutputTemplate = $"[{{@t:yyyy-MM-dd HH:mm:ss.fff}} {{@l:u3}}]{categoryTemplate}{jobNameTemplate}{instanceNameTemplate} {{@m}}\n{{@x}}";
        const string fileOutputTemplate = $"{{@t:yyyy-MM-dd HH:mm:ss.fff zzz}} [{{@l:u3}}]{categoryTemplate}{jobNameTemplate}{instanceNameTemplate} {{@m:lj}}\n{{@x}}";

        // Determine categories and padding sizes
        List<string> categories = ["SYSTEM", "API", "JOBS", "NOTIFICATIONS"];
        int catPadding = categories.Max(x => x.Length) + 2;

        // Determine job name padding
        List<string> jobNames = [nameof(ContentBlocker), nameof(QueueCleaner), nameof(DownloadCleaner)];
        int jobPadding = jobNames.Max(x => x.Length) + 2;

        // Determine instance name padding
        List<string> instanceNames = [InstanceType.Sonarr.ToString(), InstanceType.Radarr.ToString(), InstanceType.Lidarr.ToString()];
        int arrPadding = instanceNames.Max(x => x.Length) + 2;

        // Set the minimum log level
        LogEventLevel level = config?.LogLevel ?? LogEventLevel.Information;

        // Apply padding values to templates
        string consoleTemplate = consoleOutputTemplate
            .Replace("CAT_PAD", catPadding.ToString())
            .Replace("JOB_PAD", jobPadding.ToString())
            .Replace("ARR_PAD", arrPadding.ToString());

        string fileTemplate = fileOutputTemplate
            .Replace("CAT_PAD", catPadding.ToString())
            .Replace("JOB_PAD", jobPadding.ToString())
            .Replace("ARR_PAD", arrPadding.ToString());

        // Configure base logger
        logConfig
            .MinimumLevel.Is(level)
            .Enrich.FromLogContext()
            .WriteTo.Console(new ExpressionTemplate(consoleTemplate, theme: TemplateTheme.Literate));

        // Add main log file
        logConfig.WriteTo.File(
            path: Path.Combine(logsPath, "cleanuperr-.txt"),
            formatter: new ExpressionTemplate(fileTemplate),
            fileSizeLimitBytes: 10L * 1024 * 1024,
            rollingInterval: RollingInterval.Day,
            rollOnFileSizeLimit: true
        );

        // Add category-specific log files
        foreach (var category in categories)
        {
            logConfig.WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e =>
                    e.Properties.TryGetValue("Category", out var prop) &&
                    prop.ToString().Contains(category, StringComparison.OrdinalIgnoreCase))
                .WriteTo.File(
                    path: Path.Combine(logsPath, $"{category.ToLower()}-.txt"),
                    formatter: new ExpressionTemplate(fileTemplate),
                    fileSizeLimitBytes: 5L * 1024 * 1024,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true
                )
            );
        }

        // Configure SignalR log sink if enabled
        if (config?.SignalR?.Enabled != false)
        {
            var bufferSize = config?.SignalR?.BufferSize ?? 100;
            
            // Create and register LogBuffer
            var logBuffer = new LogBuffer(bufferSize);
            serviceProvider.GetRequiredService<IServiceCollection>().AddSingleton(logBuffer);
            
            // Create a log sink for SignalR
            logConfig.WriteTo.Sink(new DeferredSignalRSink());
        }
        
        Log.Logger = logConfig
            .MinimumLevel.Override("MassTransit", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Warning)
            .MinimumLevel.Override("Quartz", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Error)
            .Enrich.WithProperty("ApplicationName", "cleanuperr")
            .CreateLogger();
        
        return builder
            .ClearProviders()
            .AddSerilog(dispose: true);
    }
}