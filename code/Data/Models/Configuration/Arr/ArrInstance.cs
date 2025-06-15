using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Common.Attributes;

namespace Data.Models.Configuration.Arr;

public sealed class ArrInstance
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ArrConfigId { get; set; }
    
    public ArrConfig? ArrConfig { get; set; }
    
    public required string Name { get; set; }
    
    public required Uri Url { get; set; }
    
    [SensitiveData]
    public required string ApiKey { get; set; }
}