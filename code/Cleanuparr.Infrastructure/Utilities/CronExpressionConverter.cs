using System.ComponentModel.DataAnnotations;
using Cleanuparr.Infrastructure.Models;
using Quartz;

namespace Cleanuparr.Infrastructure.Utilities;

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

        // Validate the schedule using predefined valid values
        if (!ScheduleOptions.IsValidValue(schedule.Type, schedule.Every))
        {
            var validValues = string.Join(", ", ScheduleOptions.GetValidValues(schedule.Type));
            throw new ValidationException($"Invalid value for {schedule.Type}: {schedule.Every}. Valid values are: {validValues}");
        }

        // Cron format: Seconds Minutes Hours Day-of-month Month Day-of-week Year
        return schedule.Type switch
        {
            ScheduleUnit.Seconds => 
                $"0/{schedule.Every} * * ? * * *", // Every n seconds
            
            ScheduleUnit.Minutes => 
                $"0 0/{schedule.Every} * ? * * *", // Every n minutes
            
            ScheduleUnit.Hours => 
                $"0 0 0/{schedule.Every} ? * * *", // Every n hours
            
            _ => throw new ArgumentException($"Invalid schedule unit: {schedule.Type}")
        };
    }
    
    /// <summary>
    /// Validates a cron expression string to ensure it's valid for Quartz.NET
    /// </summary>
    /// <param name="cronExpression">The cron expression to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidCronExpression(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            return false;
            
        try
        {
            return CronExpression.IsValidExpression(cronExpression);
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Try to get a user-friendly description of a cron expression
    /// </summary>
    /// <param name="cronExpression">The cron expression to describe</param>
    /// <returns>A human-readable description or null if not valid</returns>
    public static string? GetCronDescription(string cronExpression)
    {
        if (!IsValidCronExpression(cronExpression))
            return null;
            
        try
        {
            var expression = new CronExpression(cronExpression);
            // This is a simplified description - a proper implementation would use
            // a library like CronExpressionDescriptor to provide a better description
            return $"Custom schedule: {cronExpression}";
        }
        catch
        {
            return null;
        }
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
