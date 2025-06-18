using Cleanuparr.Infrastructure.Models;

namespace Cleanuparr.Infrastructure.Utilities;

/// <summary>
/// Provides predefined valid scheduling options for different time units
/// </summary>
public static class ScheduleOptions
{
    /// <summary>
    /// Valid second values (only 30 seconds is allowed)
    /// </summary>
    public static readonly int[] ValidSecondValues = { 30 };

    /// <summary>
    /// Valid minute values (values that divide evenly into 60)
    /// </summary>
    public static readonly int[] ValidMinuteValues = { 1, 2, 3, 4, 5, 6, 10, 12, 15, 20, 30 };

    /// <summary>
    /// Valid hour values (values that divide evenly into 24)
    /// </summary>
    public static readonly int[] ValidHourValues = { 1, 2, 3, 4, 6, 8, 12 };

    /// <summary>
    /// Get valid scheduling values for a given time unit
    /// </summary>
    /// <param name="unit">The time unit</param>
    /// <returns>Array of valid values for the given unit</returns>
    public static int[] GetValidValues(ScheduleUnit unit)
    {
        return unit switch
        {
            ScheduleUnit.Seconds => ValidSecondValues,
            ScheduleUnit.Minutes => ValidMinuteValues,
            ScheduleUnit.Hours => ValidHourValues,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown schedule unit")
        };
    }

    /// <summary>
    /// Checks if a value is valid for a given time unit
    /// </summary>
    /// <param name="unit">The time unit</param>
    /// <param name="value">The value to check</param>
    /// <returns>True if the value is valid for the given unit, false otherwise</returns>
    public static bool IsValidValue(ScheduleUnit unit, int value)
    {
        return GetValidValues(unit).Contains(value);
    }
}
