namespace Common.Configuration;

public interface IJobConfig : IConfig
{
    bool Enabled { get; init; }
    
    string CronExpression { get; init; }
}