using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration;

public interface IConfiguration
{
    string? this[string key] { get; set; }

    IConfigurationSection GetSection(string key);

    IEnumerable<IConfigurationSection> GetChildren();

    IChangeToken GetReloadToken();
}

public interface IConfigurationSection : IConfiguration
{
    string Key { get; }

    string Path { get; }

    string? Value { get; set; }
}

public interface IConfigurationBuilder
{
    IDictionary<string, object> Properties { get; }

    IList<IConfigurationSource> Sources { get; }

    IConfigurationBuilder Add(IConfigurationSource source);

    IConfigurationRoot Build();
}

public interface IConfigurationManager : IConfiguration, IConfigurationBuilder
{
}

public interface IConfigurationRoot : IConfiguration
{
    void Reload();

    IEnumerable<IConfigurationProvider> Providers { get; }
}

public interface IConfigurationProvider
{
    bool TryGet(string key, out string? value);

    void Set(string key, string? value);

    IChangeToken GetReloadToken();

    void Load();

    IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath);
}

public interface IConfigurationSource
{
    IConfigurationProvider Build(IConfigurationBuilder builder);
}
