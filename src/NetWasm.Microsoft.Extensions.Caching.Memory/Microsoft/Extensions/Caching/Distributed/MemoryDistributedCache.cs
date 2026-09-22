using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Distributed;

public sealed class MemoryDistributedCache : IDistributedCache, IDisposable
{
    private readonly MemoryCache _cache;

    public MemoryDistributedCache(IOptions<MemoryDistributedCacheOptions> optionsAccessor)
    {
        ArgumentNullException.ThrowIfNull(optionsAccessor);
        _cache = new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(optionsAccessor.Value));
    }

    public byte[]? Get(string key) => _cache.Get<byte[]>(key);

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);
        _cache.Set(key, value, ToMemoryOptions(options, value.LongLength));
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }

    public void Refresh(string key)
    {
        _ = Get(key);
    }

    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        Refresh(key);
        return Task.CompletedTask;
    }

    public void Remove(string key) => _cache.Remove(key);

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    public void Dispose() => _cache.Dispose();

    private static MemoryCacheEntryOptions ToMemoryOptions(DistributedCacheEntryOptions options, long size)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = options.AbsoluteExpiration,
            AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow,
            SlidingExpiration = options.SlidingExpiration,
            Size = size,
        };
    }
}
