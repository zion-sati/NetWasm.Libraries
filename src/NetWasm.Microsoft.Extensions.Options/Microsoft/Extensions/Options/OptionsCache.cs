using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Options;

/// <summary>Ordinal named-options cache used by <see cref="OptionsMonitor{TOptions}"/>.</summary>
public class OptionsCache<TOptions> : IOptionsMonitorCache<TOptions> where TOptions : class
{
    private readonly object _gate = new();
    private readonly Dictionary<string, TOptions> _cache = new(StringComparer.Ordinal);

    public void Clear()
    {
        lock (_gate)
        {
            _cache.Clear();
        }
    }

    public virtual TOptions GetOrAdd(string? name, Func<TOptions> createOptions)
    {
        ArgumentNullException.ThrowIfNull(createOptions);
        var normalizedName = name ?? Options.DefaultName;
        lock (_gate)
        {
            if (_cache.TryGetValue(normalizedName, out var options))
            {
                return options;
            }

            options = createOptions();
            _cache.Add(normalizedName, options);
            return options;
        }
    }

    public virtual bool TryAdd(string? name, TOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        lock (_gate)
        {
            return _cache.TryAdd(name ?? Options.DefaultName, options);
        }
    }

    public virtual bool TryRemove(string? name)
    {
        lock (_gate)
        {
            return _cache.Remove(name ?? Options.DefaultName);
        }
    }
}
