using Cleanuparr.Infrastructure.Models;

namespace Infrastructure.Services.Interfaces;

public interface IJobManagementService
{
    Task<bool> StartJob(JobType jobType, JobSchedule? schedule = null, string? directCronExpression = null);
    Task<bool> StopJob(JobType jobType);
    Task<bool> PauseJob(JobType jobType);
    Task<bool> ResumeJob(JobType jobType);
    Task<IReadOnlyList<JobInfo>> GetAllJobs();
    Task<JobInfo> GetJob(JobType jobType);
    Task<bool> UpdateJobSchedule(JobType jobType, JobSchedule schedule);
}