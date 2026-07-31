using Microsoft.Extensions.Configuration;

namespace MYA.RequestProtect.Tests.Setup;

internal sealed class ReloadableConfigurationProvider : ConfigurationProvider
{
    public void Update(IDictionary<string, string?> newData)
    {
        Data = new Dictionary<string, string?>(newData, StringComparer.OrdinalIgnoreCase);
        OnReload();
    }
}

internal sealed class ReloadableConfigurationSource : IConfigurationSource
{
    public ReloadableConfigurationProvider Provider { get; } = new();

    public IConfigurationProvider Build(IConfigurationBuilder builder) => Provider;
}
