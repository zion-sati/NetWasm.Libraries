using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration;

public class ConfigurationBuilder : IConfigurationBuilder
{
    private readonly List<IConfigurationSource> _sources = new();

    public IList<IConfigurationSource> Sources => _sources;

    public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    public IConfigurationBuilder Add(IConfigurationSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _sources.Add(source);
        return this;
    }

    public IConfigurationRoot Build()
    {
        var providers = new List<IConfigurationProvider>(_sources.Count);
        foreach (IConfigurationSource source in _sources) providers.Add(source.Build(this));
        return new ConfigurationRoot(providers);
    }
}
