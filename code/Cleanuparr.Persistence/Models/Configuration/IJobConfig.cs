namespace Cleanuparr.Persistence.Models.Configuration;

public interface IJobConfig : IConfig
{
    bool Enabled { get; init; }
    
    string CronExpression { get; init; }
    
    /// <summary>
    /// Indicates whether to use the CronExpression directly (true) or convert from JobSchedule (false)
    /// </summary>
    bool UseAdvancedScheduling { get; init; }
}