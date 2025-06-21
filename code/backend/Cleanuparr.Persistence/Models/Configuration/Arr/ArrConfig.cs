using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cleanuparr.Domain.Enums;

namespace Cleanuparr.Persistence.Models.Configuration.Arr;

public class ArrConfig : IConfig
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public required InstanceType Type { get; set; }
    
    public short FailedImportMaxStrikes { get; set; } = -1;

    public List<ArrInstance> Instances { get; set; } = [];

    public void Validate()
    {
    }
}