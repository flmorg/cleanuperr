using Cleanuparr.Persistence.Models.Configuration.QueueCleaner;

namespace Cleanuparr.Api.Features.QueueCleaner.Contracts.Responses;

public sealed record ErroredConfigResponse
{
    public ushort MaxStrikes { get; init; }

    public bool IgnorePrivate { get; init; }

    public bool DeletePrivate { get; init; }

    public bool ChangeCategory { get; init; }

    public static ErroredConfigResponse From(ErroredConfig config) => new()
    {
        MaxStrikes = config.MaxStrikes,
        IgnorePrivate = config.IgnorePrivate,
        DeletePrivate = config.DeletePrivate,
        ChangeCategory = config.ChangeCategory,
    };
}
