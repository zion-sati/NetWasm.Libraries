// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Options;

/// <summary>Provides configured options with a scope-local named cache.</summary>
public class OptionsManager<TOptions> : IOptions<TOptions>, IOptionsSnapshot<TOptions> where TOptions : class
{
    private readonly IOptionsFactory<TOptions> _factory;
    private readonly OptionsCache<TOptions> _cache = new();

    public OptionsManager(IOptionsFactory<TOptions> factory) => _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    public TOptions Value => Get(Options.DefaultName);

    public virtual TOptions Get(string? name) =>
        _cache.GetOrAdd(name, () => _factory.Create(name ?? Options.DefaultName));
}

/// <summary>Provides configured options and invalidates named values from change tokens.</summary>
public class OptionsMonitor<TOptions> : IOptionsMonitor<TOptions>, IDisposable where TOptions : class
{
    private readonly IOptionsFactory<TOptions> _factory;
    private readonly IOptionsMonitorCache<TOptions> _cache;
    private readonly List<IDisposable> _registrations = new();
    private readonly object _listenerGate = new();
    private Action<TOptions, string?>? _onChange;

    public OptionsMonitor(IOptionsFactory<TOptions> factory)
        : this(factory, Array.Empty<IOptionsChangeTokenSource<TOptions>>(), new OptionsCache<TOptions>())
    {
    }

    public OptionsMonitor(
        IOptionsFactory<TOptions> factory,
        IEnumerable<IOptionsChangeTokenSource<TOptions>> sources,
        IOptionsMonitorCache<TOptions> cache)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        ArgumentNullException.ThrowIfNull(sources);
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        foreach (var source in sources)
        {
            ArgumentNullException.ThrowIfNull(source);
            _registrations.Add(ChangeToken.OnChange(source.GetChangeToken, InvokeChanged, source.Name));
        }
    }

    public TOptions CurrentValue => Get(Options.DefaultName);

    public virtual TOptions Get(string? name) =>
        _cache.GetOrAdd(name, () => _factory.Create(name ?? Options.DefaultName));

    public IDisposable? OnChange(Action<TOptions, string?> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        var subscription = new ChangeSubscription(this, listener);
        lock (_listenerGate)
        {
            _onChange += subscription.Invoke;
        }

        return subscription;
    }

    public void Dispose()
    {
        foreach (var registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
        lock (_listenerGate)
        {
            _onChange = null;
        }
    }

    private void InvokeChanged(string? name)
    {
        var normalizedName = name ?? Options.DefaultName;
        _cache.TryRemove(normalizedName);
        var options = Get(normalizedName);
        Action<TOptions, string?>? listeners;
        lock (_listenerGate)
        {
            listeners = _onChange;
        }

        listeners?.Invoke(options, normalizedName);
    }

    private sealed class ChangeSubscription : IDisposable
    {
        private readonly OptionsMonitor<TOptions> _monitor;
        private readonly Action<TOptions, string?> _listener;
        private bool _disposed;

        public ChangeSubscription(OptionsMonitor<TOptions> monitor, Action<TOptions, string?> listener)
        {
            _monitor = monitor;
            _listener = listener;
        }

        public void Invoke(TOptions options, string? name) => _listener(options, name);

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            lock (_monitor._listenerGate)
            {
                _monitor._onChange -= Invoke;
            }
        }
    }
}
