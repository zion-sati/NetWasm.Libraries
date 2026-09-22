using System;

namespace Microsoft.Extensions.Caching.Memory;

public interface IMemoryCache : IDisposable
{
    bool TryGetValue(object key, out object? value);

    ICacheEntry CreateEntry(object key);

    void Remove(object key);
}
