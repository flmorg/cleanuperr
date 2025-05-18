using Infrastructure.Models;

namespace Infrastructure.Services.Interfaces;

public interface IJobManagementService
{
    Task<bool> StartJob(string jobName, string? cronExpression = null);
    Task<bool> StopJob(string jobName);
    Task<bool> PauseJob(string jobName);
    Task<bool> ResumeJob(string jobName);
    Task<IReadOnlyList<JobInfo>> GetAllJobs();
    Task<JobInfo> GetJob(string jobName);
    Task<bool> UpdateJobSchedule(string jobName, string cronExpression);
}