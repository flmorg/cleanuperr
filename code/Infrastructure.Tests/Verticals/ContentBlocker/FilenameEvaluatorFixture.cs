using Infrastructure.Verticals.ContentBlocker;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Infrastructure.Tests.Verticals.ContentBlocker;

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