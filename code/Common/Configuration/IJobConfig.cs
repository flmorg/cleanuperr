namespace Common.Configuration;

public interface IJobConfig : IConfig
{
    bool Enabled { get; init; }
}