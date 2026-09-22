using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory;

public class MemoryCacheEntryOptions
{
    private TimeSpan? _absoluteExpirationRelativeToNow;
    private TimeSpan? _slidingExpiration;
    private long? _size;

    public DateTimeOffset? AbsoluteExpiration { get; set; }

    public TimeSpan? AbsoluteExpirationRelativeToNow
    {
        get => _absoluteExpirationRelativeToNow;
        set
        {
            if (value <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(value), "Expiration must be positive.");
            _absoluteExpirationRelativeToNow = value;
        }
    }

    public TimeSpan? SlidingExpiration
    {
        get => _slidingExpiration;
        set
        {
            if (value <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(value), "Expiration must be positive.");
            _slidingExpiration = value;
        }
    }

    public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();

    public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = new List<PostEvictionCallbackRegistration>();

    public CacheItemPriority Priority { get; set; } = CacheItemPriority.Normal;

    public long? Size
    {
        get => _size;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Size must be non-negative.");
            _size = value;
        }
    }
}
