using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Common.Configuration.QueueCleaner;

namespace Infrastructure.Verticals.ContentBlocker;

public interface IFilenameEvaluator
{
    bool IsValid(string filename, BlocklistType type, ConcurrentBag<string> patterns, ConcurrentBag<Regex> regexes);
}