using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Distributed;

public static class DistributedCacheExtensions
{
    public static string? GetString(this IDistributedCache cache, string key)
    {
        byte[]? bytes = cache.Get(key);
        return bytes is null ? null : Encoding.UTF8.GetString(bytes);
    }

    public static Task<string?> GetStringAsync(this IDistributedCache cache, string key, CancellationToken token = default) => GetStringAsyncCore(cache, key, token);

    private static async Task<string?> GetStringAsyncCore(IDistributedCache cache, string key, CancellationToken token)
    {
        byte[]? bytes = await cache.GetAsync(key, token).ConfigureAwait(false);
        return bytes is null ? null : Encoding.UTF8.GetString(bytes);
    }

    public static void SetString(this IDistributedCache cache, string key, string value) => cache.Set(key, Encoding.UTF8.GetBytes(value), new DistributedCacheEntryOptions());

    public static void SetString(this IDistributedCache cache, string key, string value, DistributedCacheEntryOptions options) => cache.Set(key, Encoding.UTF8.GetBytes(value), options);

    public static Task SetStringAsync(this IDistributedCache cache, string key, string value, CancellationToken token = default) =>
        cache.SetAsync(key, Encoding.UTF8.GetBytes(value), new DistributedCacheEntryOptions(), token);

    public static Task SetStringAsync(this IDistributedCache cache, string key, string value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
        cache.SetAsync(key, Encoding.UTF8.GetBytes(value), options, token);
}

public static class DistributedCacheEntryExtensions
{
    public static DistributedCacheEntryOptions SetAbsoluteExpiration(this DistributedCacheEntryOptions options, DateTimeOffset absolute) { options.AbsoluteExpiration = absolute; return options; }
    public static DistributedCacheEntryOptions SetAbsoluteExpiration(this DistributedCacheEntryOptions options, TimeSpan relative) { options.AbsoluteExpirationRelativeToNow = relative; return options; }
    public static DistributedCacheEntryOptions SetSlidingExpiration(this DistributedCacheEntryOptions options, TimeSpan offset) { options.SlidingExpiration = offset; return options; }
}
