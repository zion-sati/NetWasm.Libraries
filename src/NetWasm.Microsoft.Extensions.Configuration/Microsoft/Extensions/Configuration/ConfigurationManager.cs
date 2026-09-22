using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration;

public sealed class ConfigurationManager : IConfigurationManager, IConfigurationRoot, IDisposable
{
    private readonly ConfigurationSourceCollection _sources;
    private readonly Dictionary<string, object> _properties = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConfigurationRoot _root;

    public ConfigurationManager()
    {
        _root = new ConfigurationRoot(new List<IConfigurationProvider>());
        _sources = new ConfigurationSourceCollection(this);
        _sources.Add(new MemoryConfigurationSource());
    }

    public string? this[string key]
    {
        get => _root[key];
        set => _root[key] = value;
    }

    public IList<IConfigurationSource> Sources => _sources;

    IDictionary<string, object> IConfigurationBuilder.Properties => _properties;

    IEnumerable<IConfigurationProvider> IConfigurationRoot.Providers => _root.Providers;

    public IConfigurationSection GetSection(string key) => _root.GetSection(key);

    public IEnumerable<IConfigurationSection> GetChildren() => _root.GetChildren();

    IChangeToken IConfiguration.GetReloadToken() => _root.GetReloadToken();

    IConfigurationBuilder IConfigurationBuilder.Add(IConfigurationSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _sources.Add(source);
        return this;
    }

    IConfigurationRoot IConfigurationBuilder.Build() => this;

    void IConfigurationRoot.Reload() => _root.Reload();

    public void Dispose() => _root.Dispose();

    private void InsertSource(int index, IConfigurationSource source) =>
        _root.InsertProvider(index, source.Build(this));

    private void RemoveSource(int index) => _root.RemoveProviderAt(index);

    private sealed class ConfigurationSourceCollection : Collection<IConfigurationSource>
    {
        private readonly ConfigurationManager _owner;

        internal ConfigurationSourceCollection(ConfigurationManager owner) => _owner = owner;

        protected override void InsertItem(int index, IConfigurationSource item)
        {
            ArgumentNullException.ThrowIfNull(item);
            _owner.InsertSource(index, item);
            base.InsertItem(index, item);
            _owner._root.RaiseProvidersChanged();
        }

        protected override void SetItem(int index, IConfigurationSource item)
        {
            ArgumentNullException.ThrowIfNull(item);
            IConfigurationProvider replacement = item.Build(_owner);
            _owner._root.ReplaceProvider(index, replacement);
            base.SetItem(index, item);
            _owner._root.RaiseProvidersChanged();
        }

        protected override void RemoveItem(int index)
        {
            _owner.RemoveSource(index);
            base.RemoveItem(index);
            _owner._root.RaiseProvidersChanged();
        }

        protected override void ClearItems()
        {
            for (int index = Count - 1; index >= 0; index--)
                _owner.RemoveSource(index);
            base.ClearItems();
            _owner._root.RaiseProvidersChanged();
        }
    }
}
