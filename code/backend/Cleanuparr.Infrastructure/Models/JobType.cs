namespace Cleanuparr.Infrastructure.Models;

/// <summary>
/// Represents the supported job types in the application
/// </summary>
public enum JobType
{
    QueueCleaner,
    ContentBlocker,
    DownloadCleaner
}

/// <summary>
/// Extension methods for JobType enum
/// </summary>
public static class JobTypeExtensions
{
    /// <summary>
    /// Converts a JobType enum to its string representation
    /// </summary>
    /// <param name="jobType">The job type to convert</param>
    /// <returns>String representation of the job type</returns>
    public static string ToJobName(this JobType jobType) => jobType.ToString();

    /// <summary>
    /// Parses a string to JobType enum
    /// </summary>
    /// <param name="jobName">The job name to parse</param>
    /// <returns>JobType if successful, null if parsing failed</returns>
    public static JobType? TryParseJobType(string jobName)
    {
        if (string.IsNullOrEmpty(jobName))
            return null;

        return Enum.TryParse<JobType>(jobName, true, out var result) ? result : null;
    }
}
