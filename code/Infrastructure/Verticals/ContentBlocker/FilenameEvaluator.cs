using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verticals.ContentBlocker;

public sealed class FilenameEvaluator
{
    private readonly ILogger<FilenameEvaluator> _logger;
    private readonly BlocklistProvider _blocklistProvider;
    
    public FilenameEvaluator(ILogger<FilenameEvaluator> logger, BlocklistProvider blocklistProvider)
    {
        _logger = logger;
        _blocklistProvider = blocklistProvider;
    }
    
    // TODO create unit tests
    public bool IsValid(string filename)
    {
        return IsValidAgainstPatterns(filename) && IsValidAgainstRegexes(filename);
    }

    private bool IsValidAgainstPatterns(string filename)
    {
        if (_blocklistProvider.Patterns.Count is 0)
        {
            return true;
        }

        return _blocklistProvider.BlocklistType switch
        {
            BlocklistType.Blacklist => !_blocklistProvider.Patterns.Any(pattern => MatchesPattern(filename, pattern)),
            BlocklistType.Whitelist => _blocklistProvider.Patterns.Any(pattern => MatchesPattern(filename, pattern)),
            _ => true
        };
    }

    private bool IsValidAgainstRegexes(string filename)
    {
        if (_blocklistProvider.Regexes.Count is 0)
        {
            return true;
        }
        
        return _blocklistProvider.BlocklistType switch
        {
            BlocklistType.Blacklist => !_blocklistProvider.Regexes.Any(regex => regex.IsMatch(filename)),
            BlocklistType.Whitelist => _blocklistProvider.Regexes.Any(regex => regex.IsMatch(filename)),
            _ => true
        };
    }
    
    private static bool MatchesPattern(string filename, string pattern)
    {
        bool hasStartWildcard = pattern.StartsWith('*');
        bool hasEndWildcard = pattern.EndsWith('*');

        if (hasStartWildcard && hasEndWildcard)
        {
            return filename.Contains(
                pattern.Substring(1, pattern.Length - 2),
                StringComparison.InvariantCultureIgnoreCase
            );
        }

        if (hasStartWildcard)
        {
            return filename.EndsWith(pattern.Substring(1), StringComparison.InvariantCultureIgnoreCase);
        }

        if (hasEndWildcard)
        {
            return filename.StartsWith(
                pattern.Substring(0, pattern.Length - 1),
                StringComparison.InvariantCultureIgnoreCase
            );
        }

        return filename == pattern;
    }
}