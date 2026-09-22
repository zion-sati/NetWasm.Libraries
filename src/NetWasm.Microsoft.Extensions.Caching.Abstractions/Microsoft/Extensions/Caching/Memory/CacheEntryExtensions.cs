using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory;

public static class CacheEntryExtensions
{
    public static ICacheEntry SetPriority(this ICacheEntry entry, CacheItemPriority priority) { entry.Priority = priority; return entry; }

    public static ICacheEntry AddExpirationToken(this ICacheEntry entry, IChangeToken expirationToken)
    {
        ArgumentNullException.ThrowIfNull(expirationToken);
        entry.ExpirationTokens.Add(expirationToken);
        return entry;
    }

    public static ICacheEntry SetAbsoluteExpiration(this ICacheEntry entry, TimeSpan relative) { entry.AbsoluteExpirationRelativeToNow = relative; return entry; }

    public static ICacheEntry SetAbsoluteExpiration(this ICacheEntry entry, DateTimeOffset absolute) { entry.AbsoluteExpiration = absolute; return entry; }

    public static ICacheEntry SetSlidingExpiration(this ICacheEntry entry, TimeSpan offset) { entry.SlidingExpiration = offset; return entry; }

    public static ICacheEntry RegisterPostEvictionCallback(this ICacheEntry entry, PostEvictionDelegate callback) =>
        entry.RegisterPostEvictionCallback(callback, null);

    public static ICacheEntry RegisterPostEvictionCallback(this ICacheEntry entry, PostEvictionDelegate callback, object? state)
    {
        ArgumentNullException.ThrowIfNull(callback);
        entry.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration { EvictionCallback = callback, State = state });
        return entry;
    }

    public static ICacheEntry SetValue(this ICacheEntry entry, object? value) { entry.Value = value; return entry; }

    public static ICacheEntry SetSize(this ICacheEntry entry, long size) { entry.Size = size; return entry; }

    public static ICacheEntry SetOptions(this ICacheEntry entry, MemoryCacheEntryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        entry.AbsoluteExpiration = options.AbsoluteExpiration;
        entry.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
        entry.SlidingExpiration = options.SlidingExpiration;
        entry.Priority = options.Priority;
        entry.Size = options.Size;
        foreach (IChangeToken token in options.ExpirationTokens) entry.ExpirationTokens.Add(token);
        foreach (PostEvictionCallbackRegistration callback in options.PostEvictionCallbacks)
        {
            if (callback.EvictionCallback is null) throw new ArgumentException("An eviction callback cannot be null.", nameof(options));
            entry.PostEvictionCallbacks.Add(callback);
        }
        return entry;
    }
}
