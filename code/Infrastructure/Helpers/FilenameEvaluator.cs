using Common.Configuration.ContentBlocker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Helpers;

public sealed class FilenameEvaluator
{
    enum PatternType
    {
        None,
        Blacklist,
        Whitelist
    }

    private readonly ILogger<FilenameEvaluator> _logger;
    private readonly ContentBlockerConfig _config;
    private readonly PatternType _patternType;
    private string[]? _patterns;
    
    public FilenameEvaluator(ILogger<FilenameEvaluator> logger, IOptions<ContentBlockerConfig> config)
    {
        _logger = logger;
        _config = config.Value;
        
        _config.Validate();
        
        if (_config.Blacklist?.Enabled is true)
        {
            _patternType = PatternType.Blacklist;
        }

        if (_config.Whitelist?.Enabled is true)
        {
            _patternType = PatternType.Whitelist;
        }
    }
    
    public async Task LoadPatterns()
    {
        if (_patterns is not null)
        {
            return;
        }

        try
        {
            if (_patternType is PatternType.Blacklist)
            {
                _patterns = await File.ReadAllLinesAsync(_config.Blacklist.Path);
            }

            if (_patternType is PatternType.Whitelist)
            {
                _patterns = await File.ReadAllLinesAsync(_config.Whitelist.Path);
            }
        }
        catch
        {
            _logger.LogError("failed to load {type}", _patternType.ToString());
            throw;
        }
    }

    // TODO create unit tests
    public bool IsValid(string filename)
    {
        if (_patterns?.Length is null or 0)
        {
            _logger.LogDebug("patterns are not loaded");
            return true;
        }

        return _patternType switch
        {
            PatternType.Blacklist => !_patterns.Any(pattern => MatchesPattern(filename, pattern)),
            PatternType.Whitelist => _patterns.Any(pattern => MatchesPattern(filename, pattern)),
            _ => true
        };
    }
    
    bool MatchesPattern(string filename, string pattern)
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