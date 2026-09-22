using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory;

public static class MemoryCacheEntryExtensions
{
    public static MemoryCacheEntryOptions SetPriority(this MemoryCacheEntryOptions options, CacheItemPriority priority) { options.Priority = priority; return options; }

    public static MemoryCacheEntryOptions AddExpirationToken(this MemoryCacheEntryOptions options, IChangeToken token) { ArgumentNullException.ThrowIfNull(token); options.ExpirationTokens.Add(token); return options; }

    public static MemoryCacheEntryOptions SetAbsoluteExpiration(this MemoryCacheEntryOptions options, TimeSpan relative) { options.AbsoluteExpirationRelativeToNow = relative; return options; }

    public static MemoryCacheEntryOptions SetAbsoluteExpiration(this MemoryCacheEntryOptions options, DateTimeOffset absolute) { options.AbsoluteExpiration = absolute; return options; }

    public static MemoryCacheEntryOptions SetSlidingExpiration(this MemoryCacheEntryOptions options, TimeSpan offset) { options.SlidingExpiration = offset; return options; }

    public static MemoryCacheEntryOptions SetSize(this MemoryCacheEntryOptions options, long size) { options.Size = size; return options; }

    public static MemoryCacheEntryOptions RegisterPostEvictionCallback(this MemoryCacheEntryOptions options, PostEvictionDelegate callback) => options.RegisterPostEvictionCallback(callback, null);

    public static MemoryCacheEntryOptions RegisterPostEvictionCallback(this MemoryCacheEntryOptions options, PostEvictionDelegate callback, object? state)
    {
        ArgumentNullException.ThrowIfNull(callback);
        options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration { EvictionCallback = callback, State = state });
        return options;
    }
}
