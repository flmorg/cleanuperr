using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Configuration.Arr;

public abstract class ArrConfig : IConfig
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; init; } = Guid.NewGuid();
    
    public bool Enabled { get; init; }

    public short FailedImportMaxStrikes { get; init; } = -1;
    
    public List<ArrInstance> Instances { get; init; } = [];

    public abstract void Validate();
}