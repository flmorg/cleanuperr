using System.ComponentModel.DataAnnotations.Schema;
using Cleanuparr.Domain.Exceptions;

namespace Cleanuparr.Persistence.Models.Configuration.QueueCleaner;

[ComplexType]
public sealed record ErroredConfig
{
    /// <summary>
    /// Strikes before a download the client reports as errored is removed.
    /// 0 disables the check. Not supported by rTorrent, which reports no error state.
    /// </summary>
    public ushort MaxStrikes { get; init; }

    public bool IgnorePrivate { get; init; }

    public bool DeletePrivate { get; init; }

    public bool ChangeCategory { get; init; }

    public void Validate()
    {
        if (MaxStrikes is > 0 and < 3)
        {
            throw new ValidationException("the minimum value for errored max strikes must be 3");
        }

        if (ChangeCategory && DeletePrivate)
        {
            throw new ValidationException("Cannot enable both deletion and category changing");
        }
    }
}
