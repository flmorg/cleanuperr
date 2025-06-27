using Cleanuparr.Infrastructure.Features.ContentBlocker;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Cleanuparr.Infrastructure.Tests.Verticals.ContentBlocker;

public class FilenameEvaluatorFixture
{
    public ILogger<FilenameEvaluator> Logger { get; }
    
    public FilenameEvaluatorFixture()
    {
        Logger = Substitute.For<ILogger<FilenameEvaluator>>();
    }

    public FilenameEvaluator CreateSut()
    {
        return new FilenameEvaluator(Logger);
    }
}