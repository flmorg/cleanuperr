using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Configuration.Arr;

public abstract class ArrConfig : IConfig
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public bool Enabled { get; set; }

    public short FailedImportMaxStrikes { get; set; } = -1;
    
    public List<ArrInstance> Instances { get; set; } = [];

    public abstract void Validate();
}