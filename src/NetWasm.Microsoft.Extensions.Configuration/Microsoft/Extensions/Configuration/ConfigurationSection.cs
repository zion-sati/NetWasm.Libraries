using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration;

public class ConfigurationSection : IConfigurationSection, IConfigurationSectionValueAccessor
{
    private readonly IConfigurationRoot _root;
    private string? _key;

    public ConfigurationSection(IConfigurationRoot root, string path)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public string Path { get; }

    public string Key => _key ??= ConfigurationPath.GetSectionKey(Path) ?? string.Empty;

    public string? Value
    {
        get => _root[Path];
        set => _root[Path] = value;
    }

    public string? this[string key]
    {
        get => _root[Path + ConfigurationPath.KeyDelimiter + key];
        set => _root[Path + ConfigurationPath.KeyDelimiter + key] = value;
    }

    public bool TryGetValue(string? key, out string? value)
    {
        string path = key is null ? Path : Path + ConfigurationPath.KeyDelimiter + key;
        return ConfigurationRoot.TryGetConfiguration(_root, path, out value);
    }

    bool IConfigurationSectionValueAccessor.TryGetValue(out string? value) =>
        TryGetValue(null, out value);

    public IConfigurationSection GetSection(string key) => _root.GetSection(Path + ConfigurationPath.KeyDelimiter + key);

    public IEnumerable<IConfigurationSection> GetChildren() =>
        _root is ConfigurationRoot concrete ? concrete.GetChildrenFor(Path) : Array.Empty<IConfigurationSection>();

    public IChangeToken GetReloadToken() => _root.GetReloadToken();
}
