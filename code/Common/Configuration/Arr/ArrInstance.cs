namespace Common.Configuration.Arr;

public sealed class ArrInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public required string Name { get; set; }
    
    public required Uri Url { get; set; }
    
    public required string ApiKey { get; set; }
}