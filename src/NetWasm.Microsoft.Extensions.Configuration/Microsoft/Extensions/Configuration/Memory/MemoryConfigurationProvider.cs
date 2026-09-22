using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.Memory;

public class MemoryConfigurationProvider : ConfigurationProvider, IEnumerable<KeyValuePair<string, string?>>
{
    public MemoryConfigurationProvider(MemoryConfigurationSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.InitialData is null) return;
        foreach (KeyValuePair<string, string?> pair in source.InitialData) Data[pair.Key] = pair.Value;
    }

    public void Add(string key, string? value) => Data.Add(key, value);

    public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => Data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
