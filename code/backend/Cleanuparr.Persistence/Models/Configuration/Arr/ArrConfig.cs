using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cleanuparr.Domain.Enums;
using ValidationException = Cleanuparr.Domain.Exceptions.ValidationException;

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
        if (FailedImportMaxStrikes < -1)
        {
            throw new ValidationException("Failed import max strikes must be -1 or greater");
        }

        if (FailedImportMaxStrikes is > 0 and < 3)
        {
            throw new ValidationException("The minimum value for failed import max strikes must be 3");
        }
    }
}