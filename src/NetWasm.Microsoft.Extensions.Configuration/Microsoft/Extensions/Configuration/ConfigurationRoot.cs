using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration;

public class ConfigurationRoot : IConfigurationRoot, IDisposable
{
    private readonly IList<IConfigurationProvider> _providers;
    private readonly List<IDisposable> _registrations;
    private ConfigurationReloadToken _changeToken = new();

    public ConfigurationRoot(IList<IConfigurationProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _providers = providers;
        _registrations = new List<IDisposable>(providers.Count);
        foreach (IConfigurationProvider provider in providers)
        {
            ArgumentNullException.ThrowIfNull(provider);
            provider.Load();
            _registrations.Add(ChangeToken.OnChange(provider.GetReloadToken, RaiseChanged));
        }
    }

    public IEnumerable<IConfigurationProvider> Providers => _providers;

    public string? this[string key]
    {
        get => GetConfiguration(_providers, key);
        set => SetConfiguration(_providers, key, value);
    }

    public IConfigurationSection GetSection(string key) => new ConfigurationSection(this, key);

    public IEnumerable<IConfigurationSection> GetChildren() => GetChildrenImplementation(null);

    internal IEnumerable<IConfigurationSection> GetChildrenFor(string? path) => GetChildrenImplementation(path);

    public IChangeToken GetReloadToken() => _changeToken;

    public void Reload()
    {
        foreach (IConfigurationProvider provider in _providers) provider.Load();
        RaiseChanged();
    }

    internal void AddProvider(IConfigurationProvider provider)
    {
        InsertProvider(providerIndex: _providers.Count, provider);
        RaiseChanged();
    }

    internal void InsertProvider(int providerIndex, IConfigurationProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        provider.Load();
        _providers.Insert(providerIndex, provider);
        _registrations.Insert(providerIndex, ChangeToken.OnChange(provider.GetReloadToken, RaiseChanged));
    }

    internal void RemoveProviderAt(int index)
    {
        IDisposable registration = _registrations[index];
        IConfigurationProvider provider = _providers[index];
        _registrations.RemoveAt(index);
        _providers.RemoveAt(index);
        registration.Dispose();
        (provider as IDisposable)?.Dispose();
    }

    internal void ReplaceProvider(int index, IConfigurationProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        provider.Load();
        IDisposable oldRegistration = _registrations[index];
        IConfigurationProvider oldProvider = _providers[index];
        _providers[index] = provider;
        _registrations[index] = ChangeToken.OnChange(provider.GetReloadToken, RaiseChanged);
        oldRegistration.Dispose();
        (oldProvider as IDisposable)?.Dispose();
    }

    internal void RaiseProvidersChanged() => RaiseChanged();

    public void Dispose()
    {
        foreach (IDisposable registration in _registrations) registration.Dispose();
        foreach (IConfigurationProvider provider in _providers) (provider as IDisposable)?.Dispose();
    }

    internal static string? GetConfiguration(IList<IConfigurationProvider> providers, string key)
    {
        for (int i = providers.Count - 1; i >= 0; i--)
        {
            if (providers[i].TryGet(key, out string? value)) return value;
        }

        return null;
    }

    internal static bool TryGetConfiguration(IConfigurationRoot root, string key, out string? value)
    {
        var providers = new List<IConfigurationProvider>(root.Providers);
        for (int i = providers.Count - 1; i >= 0; i--)
        {
            if (providers[i].TryGet(key, out value)) return true;
        }

        value = null;
        return false;
    }

    internal static void SetConfiguration(IList<IConfigurationProvider> providers, string key, string? value)
    {
        if (providers.Count == 0) throw new InvalidOperationException("No configuration providers are available.");
        foreach (IConfigurationProvider provider in providers) provider.Set(key, value);
    }

    private void RaiseChanged()
    {
        ConfigurationReloadToken previous = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
        previous.OnReload();
    }

    private IEnumerable<IConfigurationSection> GetChildrenImplementation(string? path)
    {
        var keys = new List<string>();
        foreach (IConfigurationProvider provider in _providers)
        {
            keys = new List<string>(provider.GetChildKeys(keys, path));
        }

        var distinct = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string key in keys)
        {
            if (distinct.Add(key))
            {
                yield return GetSection(path is null ? key : path + ConfigurationPath.KeyDelimiter + key);
            }
        }
    }
}
