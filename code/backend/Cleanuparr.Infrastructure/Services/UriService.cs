using System.Text.RegularExpressions;

namespace Cleanuparr.Infrastructure.Services;

public static class UriService
{
    public static string? GetDomain(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        // add "http://" if scheme is missing to help Uri.TryCreate
        if (!input.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            input = "http://" + input;
        }

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            return uri.Host;
        }

        // url might be malformed
        var regex = new Regex(@"^(?:https?:\/\/)?([^\/\?:]+)", RegexOptions.IgnoreCase);
        var match = regex.Match(input);
        
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        // could not extract
        return null;
    }
}