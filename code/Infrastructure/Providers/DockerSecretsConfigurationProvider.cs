using System.Collections;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Providers;

public sealed class DockerSecretsConfigurationProvider : ConfigurationProvider
{
    public override void Load()
    {
        foreach (object? variable in Environment.GetEnvironmentVariables())
        {
            if (variable is not DictionaryEntry pair)
            {
                continue;
            }
            
            string key = pair.Key.ToString()!;
            string? value = pair.Value?.ToString();

            if (!KeyHasPattern(key))
            {
                continue;
            }
                
            try
            {
                if (!string.IsNullOrEmpty(value) && File.Exists(value))
                {
                    Data[key] = File.ReadAllText(value).Trim();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"error loading secret for {key}", ex);
            }
        }
    }

    private static bool KeyHasPattern(string key)
    {
        if (key.EndsWith("USERNAME", StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }
        
        if (key.EndsWith("PASSWORD", StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }
        
        if (key.EndsWith("APIKEY", StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        if (key.EndsWith("API_KEY", StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        return false;
    }
}