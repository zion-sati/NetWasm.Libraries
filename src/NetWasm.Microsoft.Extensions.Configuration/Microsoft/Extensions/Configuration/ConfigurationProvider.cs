using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration;

public abstract class ConfigurationProvider : IConfigurationProvider
{
    private ConfigurationReloadToken _reloadToken = new();

    protected ConfigurationProvider() => Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    protected IDictionary<string, string?> Data { get; set; }

    public virtual bool TryGet(string key, out string? value) => Data.TryGetValue(key, out value);

    public virtual void Set(string key, string? value) => Data[key] = value;

    public virtual void Load()
    {
    }

    public virtual IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
    {
        var results = new List<string>();
        foreach (KeyValuePair<string, string?> pair in Data)
        {
            if (parentPath is null)
            {
                results.Add(Segment(pair.Key, 0));
            }
            else if (pair.Key.Length > parentPath.Length &&
                     pair.Key.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase) &&
                     pair.Key[parentPath.Length] == ':')
            {
                results.Add(Segment(pair.Key, parentPath.Length + 1));
            }
        }

        results.AddRange(earlierKeys);
        results.Sort(ConfigurationKeyComparer.Comparison);
        return results;
    }

    public IChangeToken GetReloadToken() => _reloadToken;

    protected void OnReload()
    {
        ConfigurationReloadToken previous = Interlocked.Exchange(ref _reloadToken, new ConfigurationReloadToken());
        previous.OnReload();
    }

    public override string ToString() => "ConfigurationProvider";

    private static string Segment(string key, int start)
    {
        int index = key.IndexOf(':', start);
        return index < 0 ? key.Substring(start) : key.Substring(start, index - start);
    }
}
