# Enhanced Logging System

## Overview

The enhanced logging system provides a structured approach to logging with the following features:

- **Category-based logging**: Organize logs by functional areas (SYSTEM, API, JOBS, etc.)
- **Job name context**: Add job name to logs for background operations
- **Instance context**: Add instance names (Sonarr, Radarr, etc.) to relevant logs
- **Multiple output targets**: Console, files, and real-time SignalR streaming
- **Category-specific log files**: Separate log files for different categories

## Using the Logging System

### Adding Category to Logs

```csharp
// Using category constants
logger.WithCategory(LoggingCategoryConstants.System)
      .LogInformation("This is a system log");

// Using direct category name
logger.WithCategory("API")
      .LogInformation("This is an API log");
```

### Adding Job Name Context

```csharp
logger.WithCategory(LoggingCategoryConstants.Jobs)
      .WithJob("ContentBlocker")
      .LogInformation("Starting content blocking job");
```

### Adding Instance Name Context

```csharp
logger.WithCategory(LoggingCategoryConstants.Sonarr)
      .WithInstance("Sonarr")
      .LogInformation("Processing Sonarr data");
```

### Combined Context Example

```csharp
logger.WithCategory(LoggingCategoryConstants.Jobs)
      .WithJob("QueueCleaner")
      .WithInstance("Radarr")
      .LogInformation("Cleaning Radarr queue");
```

## Log Storage

Logs are stored in the following locations:

- **Main log file**: `{config_path}/logs/Cleanuparr-.txt`
- **Category logs**: `{config_path}/logs/{category}-.txt` (e.g., `system-.txt`, `api-.txt`)

The log files use rolling file behavior:
- Daily rotation
- 10MB size limit for main log files
- 5MB size limit for category-specific logs

## SignalR Integration

The logging system includes real-time streaming via SignalR:

- **Hub URL**: `/hubs/logs`
- **Hub class**: `LogHub`
- **Event name**: `ReceiveLog`

### Requesting Recent Logs

When a client connects, it can request recent logs from the buffer:

```javascript
await connection.invoke("RequestRecentLogs");
```

### Log Message Format

Each log message contains:
- `timestamp`: The time the log was created
- `level`: Log level (Information, Warning, Error, etc.)
- `message`: The log message text
- `exception`: Exception details (if present)
- `category`: The log category
- `jobName`: The job name (if present)
- `instanceName`: The instance name (if present)

## How It All Works

1. The logging system is initialized during application startup
2. Logs are written to the console in real-time
3. Logs are written to files based on their category
4. Logs are buffered and sent to connected SignalR clients
5. New clients can request recent logs from the buffer

## Configuration Options

The logging configuration is loaded from the `Logging` section in appsettings.json:

```json
{
  "Logging": {
    "LogLevel": "Information",
    "SignalR": {
      "Enabled": true,
      "BufferSize": 100
    }
  }
}
```

## Standard Categories

Use the `LoggingCategoryConstants` class to ensure consistent category naming:

- `LoggingCategoryConstants.System`: System-level logs
- `LoggingCategoryConstants.Api`: API-related logs
- `LoggingCategoryConstants.Jobs`: Job execution logs
- `LoggingCategoryConstants.Notifications`: User notification logs
- `LoggingCategoryConstants.Sonarr`: Sonarr-related logs
- `LoggingCategoryConstants.Radarr`: Radarr-related logs
- `LoggingCategoryConstants.Lidarr`: Lidarr-related logs
