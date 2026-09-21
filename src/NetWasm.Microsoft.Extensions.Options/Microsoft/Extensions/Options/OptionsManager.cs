// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Options
{
    /// <summary>Provides configured options with a small in-memory named cache.</summary>
    public class OptionsManager<TOptions> : IOptions<TOptions>, IOptionsSnapshot<TOptions> where TOptions : class
    {
        private readonly IOptionsFactory<TOptions> _factory;
        private readonly System.Collections.Generic.Dictionary<string, TOptions> _cache = new(StringComparer.Ordinal);

        public OptionsManager(IOptionsFactory<TOptions> factory) => _factory = factory ?? throw new ArgumentNullException(nameof(factory));

        public TOptions Value => Get(Options.DefaultName);

        public virtual TOptions Get(string? name)
        {
            name ??= Options.DefaultName;
            if (!_cache.TryGetValue(name, out TOptions? options))
            {
                options = _factory.Create(name);
                _cache.Add(name, options);
            }

            return options;
        }
    }

    /// <summary>Provides configured options while retaining the monitor contract.</summary>
    public sealed class OptionsMonitor<TOptions> : IOptionsMonitor<TOptions>, IDisposable where TOptions : class
    {
        private readonly IOptionsFactory<TOptions> _factory;
        private readonly System.Collections.Generic.Dictionary<string, TOptions> _cache = new(StringComparer.Ordinal);

        public OptionsMonitor(IOptionsFactory<TOptions> factory) => _factory = factory ?? throw new ArgumentNullException(nameof(factory));

        public TOptions CurrentValue => Get(Options.DefaultName);

        public TOptions Get(string? name)
        {
            name ??= Options.DefaultName;
            if (!_cache.TryGetValue(name, out TOptions? options))
            {
                options = _factory.Create(name);
                _cache.Add(name, options);
            }

            return options;
        }

        // NetWasm logging intentionally uses static options. There is no configuration reload or
        // background change-token subscription in this port.
        public IDisposable? OnChange(Action<TOptions, string?> listener) => null;

        public void Dispose()
        {
            _cache.Clear();
        }
    }
}
