using Cleanuparr.Infrastructure.Models;

namespace Cleanuparr.Api.Models;

/// <summary>
/// Represents a request to schedule a job
/// </summary>
public class ScheduleRequest
{
    /// <summary>
    /// The schedule information for the job
    /// </summary>
    public JobSchedule Schedule { get; set; }
}
