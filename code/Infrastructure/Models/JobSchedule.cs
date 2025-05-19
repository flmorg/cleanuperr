using Infrastructure.Utilities;

namespace Infrastructure.Models;

/// <summary>
/// Represents the unit of time for job scheduling intervals
/// </summary>
public enum ScheduleUnit
{
    Seconds,
    Minutes,
    Hours
}

/// <summary>
/// Represents a user-friendly job schedule format
/// </summary>
public class JobSchedule
{
    /// <summary>
    /// The numeric interval value
    /// </summary>
    public int Every { get; set; }

    /// <summary>
    /// The unit of time for the interval (seconds, minutes, or hours)
    /// </summary>
    public ScheduleUnit Type { get; set; }

    /// <summary>
    /// Converts the JobSchedule to a Quartz cron expression string
    /// </summary>
    /// <returns>A valid cron expression string</returns>
    public string ToCronExpression()
    {
        return CronExpressionConverter.ConvertToCronExpression(this);
    }
}
