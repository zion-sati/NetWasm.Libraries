using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory;

public interface ICacheEntry : IDisposable
{
    object Key { get; }

    object? Value { get; set; }

    DateTimeOffset? AbsoluteExpiration { get; set; }

    TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

    TimeSpan? SlidingExpiration { get; set; }

    IList<IChangeToken> ExpirationTokens { get; }

    IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; }

    CacheItemPriority Priority { get; set; }

    long? Size { get; set; }
}
