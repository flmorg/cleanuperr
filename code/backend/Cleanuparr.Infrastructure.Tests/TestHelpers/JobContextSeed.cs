using Cleanuparr.Infrastructure.Features.Context;
using Cleanuparr.Persistence.Models.Configuration.QueueCleaner;

namespace Cleanuparr.Infrastructure.Tests.TestHelpers;

public static class JobContextSeed
{
    /// <summary>
    /// Puts the job config a download service reads into context, the way the Queue Cleaner job does.
    /// Specs that care about a setting overwrite it with their own <see cref="ContextProvider.Set(string, object)"/>.
    /// </summary>
    public static void SeedQueueCleanerConfig()
    {
        ContextProvider.Set(nameof(QueueCleanerConfig), new QueueCleanerConfig
        {
            Id = Guid.NewGuid(),
        });
    }
}
