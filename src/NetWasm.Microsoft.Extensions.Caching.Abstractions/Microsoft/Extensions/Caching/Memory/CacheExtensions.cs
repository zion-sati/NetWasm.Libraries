using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory;

public static class CacheExtensions
{
    public static object? Get(this IMemoryCache cache, object key) { cache.TryGetValue(key, out object? value); return value; }

    public static TItem? Get<TItem>(this IMemoryCache cache, object key) { cache.TryGetValue(key, out object? value); return value is null ? default : (TItem)value; }

    public static bool TryGetValue<TItem>(this IMemoryCache cache, object key, out TItem? value)
    {
        if (cache.TryGetValue(key, out object? result))
        {
            if (result is null) { value = default; return true; }
            if (result is TItem item) { value = item; return true; }
        }
        value = default;
        return false;
    }

    public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value)
    {
        using ICacheEntry entry = cache.CreateEntry(key);
        entry.Value = value;
        return value;
    }

    public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, MemoryCacheEntryOptions? options)
    {
        using ICacheEntry entry = cache.CreateEntry(key);
        if (options is not null) entry.SetOptions(options);
        entry.Value = value;
        return value;
    }

    public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, DateTimeOffset absoluteExpiration) =>
        cache.Set(key, value, new MemoryCacheEntryOptions { AbsoluteExpiration = absoluteExpiration });

    public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, TimeSpan relativeExpiration) =>
        cache.Set(key, value, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = relativeExpiration });

    public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, IChangeToken token) =>
        cache.Set(key, value, new MemoryCacheEntryOptions { ExpirationTokens = { token } });

    public static TItem? GetOrCreate<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, TItem> factory) =>
        cache.GetOrCreate(key, factory, null);

    public static TItem? GetOrCreate<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, TItem> factory, MemoryCacheEntryOptions? options)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (cache.TryGetValue(key, out object? existing)) return (TItem?)existing;
        using ICacheEntry entry = cache.CreateEntry(key);
        if (options is not null) entry.SetOptions(options);
        TItem value = factory(entry);
        entry.Value = value;
        return value;
    }

    public static Task<TItem?> GetOrCreateAsync<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, Task<TItem>> factory) =>
        cache.GetOrCreateAsync(key, factory, null);

    public static async Task<TItem?> GetOrCreateAsync<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, Task<TItem>> factory, MemoryCacheEntryOptions? options)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (cache.TryGetValue(key, out object? existing)) return (TItem?)existing;
        using ICacheEntry entry = cache.CreateEntry(key);
        if (options is not null) entry.SetOptions(options);
        TItem value = await factory(entry).ConfigureAwait(false);
        entry.Value = value;
        return value;
    }
}
