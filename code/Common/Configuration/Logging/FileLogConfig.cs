namespace Common.Configuration.Logging;

public class FileLogConfig : IConfig
{
    public bool Enabled { get; set; }
    
    public string Path { get; set; } = string.Empty;
    
    public void Validate()
    {
    }
}