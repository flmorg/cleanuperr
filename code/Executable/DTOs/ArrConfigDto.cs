using Data.Enums;

namespace Executable.DTOs;

public class ArrConfigDto
{
    public Guid Id { get; set; }
    
    public required InstanceType Type { get; set; }
    
    public bool Enabled { get; set; }

    public short FailedImportMaxStrikes { get; set; } = -1;

    public List<ArrInstanceDto> Instances { get; set; } = [];
}