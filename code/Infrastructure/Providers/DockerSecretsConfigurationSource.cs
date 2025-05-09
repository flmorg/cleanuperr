using Microsoft.Extensions.Configuration;

namespace Infrastructure.Providers;

public sealed class DockerSecretsConfigurationSource : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DockerSecretsConfigurationProvider();
    }
}