using Infrastructure.Models;

namespace Infrastructure.Utilities;

/// <summary>
/// Utility for converting user-friendly schedule formats to Quartz cron expressions
/// </summary>
public static class CronExpressionConverter
{
    /// <summary>
    /// Converts a JobSchedule to a Quartz cron expression
    /// </summary>
    /// <param name="schedule">The job schedule to convert</param>
    /// <returns>A valid Quartz cron expression</returns>
    /// <exception cref="ArgumentException">Thrown when the schedule has invalid values</exception>
    public static string ConvertToCronExpression(JobSchedule schedule)
    {
        if (schedule == null)
            throw new ArgumentNullException(nameof(schedule));

        if (schedule.Every <= 0)
            throw new ArgumentException("Every must be greater than zero", nameof(schedule.Every));

        // Cron format: Seconds Minutes Hours Day-of-month Month Day-of-week Year
        return schedule.Type switch
        {
            ScheduleUnit.Seconds when schedule.Every < 60 => 
                $"*/{schedule.Every} * * ? * * *", // Every n seconds
            
            ScheduleUnit.Minutes when schedule.Every < 60 => 
                $"0 */{schedule.Every} * ? * * *", // Every n minutes
            
            ScheduleUnit.Hours when schedule.Every < 24 => 
                $"0 0 */{schedule.Every} ? * * *", // Every n hours
            
            _ => throw new ArgumentException($"Invalid schedule: {schedule.Every} {schedule.Type}")
        };
    }

    /// <summary>
    /// This method is only kept for reference. We no longer parse schedules from strings.
    /// </summary>
    /// <param name="scheduleString">The schedule string to parse</param>
    /// <returns>A JobSchedule object if successful, null otherwise</returns>
    [Obsolete("Schedule should be provided as a proper object, not a string.")]
    private static JobSchedule? TryParseSchedule(string scheduleString)
    {
        if (string.IsNullOrEmpty(scheduleString))
            return null;

        try
        {
            // Expecting format like "every: 30, type: minutes"
            var parts = scheduleString.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return null;

            var intervalPart = parts[0].Trim();
            var typePart = parts[1].Trim();

            // Extract interval value
            var intervalValue = intervalPart.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (intervalValue.Length != 2 || !intervalValue[0].Trim().Equals("every", StringComparison.OrdinalIgnoreCase))
                return null;

            if (!int.TryParse(intervalValue[1].Trim(), out var interval) || interval <= 0)
                return null;

            // Extract unit type
            var typeParts = typePart.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (typeParts.Length != 2 || !typeParts[0].Trim().Equals("type", StringComparison.OrdinalIgnoreCase))
                return null;

            var unitString = typeParts[1].Trim();
            if (!Enum.TryParse<ScheduleUnit>(unitString, true, out var unit))
                return null;

            return new JobSchedule { Every = interval, Type = unit };
        }
        catch
        {
            return null;
        }
    }
}
