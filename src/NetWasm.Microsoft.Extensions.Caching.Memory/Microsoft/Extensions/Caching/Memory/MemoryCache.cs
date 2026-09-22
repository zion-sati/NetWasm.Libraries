using System;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory;

public class MemoryCache : IMemoryCache
{
    private readonly object _gate = new();
    private readonly Dictionary<object, CacheEntry> _entries = new();
    private readonly MemoryCacheOptions _options;
    private bool _disposed;
    private long _size;
    private long _hits;
    private long _misses;

    public MemoryCache(IOptions<MemoryCacheOptions> optionsAccessor)
    {
        ArgumentNullException.ThrowIfNull(optionsAccessor);
        _options = optionsAccessor.Value ?? throw new ArgumentException("The options value cannot be null.", nameof(optionsAccessor));
    }

    public int Count { get { lock (_gate) return _entries.Count; } }

    public IEnumerable<object> Keys
    {
        get
        {
            lock (_gate) return new List<object>(_entries.Keys);
        }
    }

    public ICacheEntry CreateEntry(object key)
    {
        ArgumentNullException.ThrowIfNull(key);
        CheckDisposed();
        return new CacheEntry(this, key);
    }

    public bool TryGetValue(object key, out object? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        List<CallbackInvocation>? callbacks = null;
        lock (_gate)
        {
            CheckDisposedLocked();
            if (!_entries.TryGetValue(key, out CacheEntry? entry) || entry.IsExpired(Now))
            {
                if (entry is not null)
                {
                    _entries.Remove(key);
                    _size -= entry.SizeValue;
                    entry.Expire(entry.HasChangedTokenValue ? EvictionReason.TokenExpired : EvictionReason.Expired);
                    callbacks = entry.TakeCallbacks();
                }
                _misses++;
                value = null;
            }
            else
            {
                entry.LastAccessed = Now;
                _hits++;
                value = entry.Value;
                return true;
            }
        }

        Invoke(callbacks);
        return false;
    }

    public void Remove(object key)
    {
        ArgumentNullException.ThrowIfNull(key);
        List<CallbackInvocation>? callbacks = null;
        lock (_gate)
        {
            CheckDisposedLocked();
            if (_entries.TryGetValue(key, out CacheEntry? entry))
            {
                _entries.Remove(key);
                _size -= entry.SizeValue;
                entry.Expire(EvictionReason.Removed);
                callbacks = entry.TakeCallbacks();
            }
        }
        Invoke(callbacks);
    }

    public void Clear()
    {
        List<CallbackInvocation> callbacks = new();
        lock (_gate)
        {
            CheckDisposedLocked();
            foreach (CacheEntry entry in _entries.Values)
            {
                entry.Expire(EvictionReason.Removed);
                callbacks.AddRange(entry.TakeCallbacks());
            }
            _entries.Clear();
            _size = 0;
        }
        Invoke(callbacks);
    }

    public void Compact(double percentage)
    {
        if (percentage < 0 || percentage > 1) throw new ArgumentOutOfRangeException(nameof(percentage));
        List<CallbackInvocation> callbacks;
        lock (_gate)
        {
            CheckDisposedLocked();
            int removeCount = (int)Math.Ceiling(_entries.Count * percentage);
            callbacks = CompactLocked(removeCount, EvictionReason.Capacity);
        }
        Invoke(callbacks);
    }

    public MemoryCacheStatistics? GetCurrentStatistics()
    {
        lock (_gate)
        {
            return _options.TrackStatistics
                ? new MemoryCacheStatistics { CurrentEntryCount = _entries.Count, CurrentEstimatedSize = _options.SizeLimit is null ? null : _size, TotalHits = _hits, TotalMisses = _misses }
                : null;
        }
    }

    internal void Commit(CacheEntry entry)
    {
        List<CallbackInvocation>? callbacks = null;
        lock (_gate)
        {
            CheckDisposedLocked();
            DateTimeOffset now = Now;
            entry.ApplyRelativeExpiration(now);
            if (entry.IsExpired(now))
            {
                entry.Expire(entry.HasChangedTokenValue ? EvictionReason.TokenExpired : EvictionReason.Expired);
                callbacks = entry.TakeCallbacks();
            }
            else if (_options.SizeLimit is not null && entry.Size is null)
            {
                throw new InvalidOperationException("All cache entries must specify a size when SizeLimit is set.");
            }
            else
            {
                if (_entries.TryGetValue(entry.Key, out CacheEntry? previous))
                {
                    _entries.Remove(entry.Key);
                    _size -= previous.SizeValue;
                    previous.Expire(EvictionReason.Replaced);
                    callbacks = previous.TakeCallbacks();
                }

                if (_options.SizeLimit is not null && !FitsWithinLimit(_size, entry.SizeValue, _options.SizeLimit.Value))
                {
                    List<CallbackInvocation> compacted = CompactToSizeLocked(entry.SizeValue, _options.SizeLimit.Value);
                    callbacks ??= new List<CallbackInvocation>();
                    callbacks.AddRange(compacted);
                }

                if (_options.SizeLimit is null || FitsWithinLimit(_size, entry.SizeValue, _options.SizeLimit.Value))
                {
                    _entries[entry.Key] = entry;
                    _size += entry.SizeValue;
                    entry.LastAccessed = now;
                    entry.AttachTokens(this);
                }
                else
                {
                    entry.Expire(EvictionReason.Capacity);
                    callbacks ??= new List<CallbackInvocation>();
                    callbacks.AddRange(entry.TakeCallbacks());
                }
            }
        }
        Invoke(callbacks);
    }

    internal void TokenExpired(CacheEntry entry)
    {
        List<CallbackInvocation>? callbacks = null;
        lock (_gate)
        {
            if (_entries.TryGetValue(entry.Key, out CacheEntry? current) && ReferenceEquals(current, entry))
            {
                _entries.Remove(entry.Key);
                _size -= entry.SizeValue;
                entry.Expire(EvictionReason.TokenExpired);
                callbacks = entry.TakeCallbacks();
            }
        }
        Invoke(callbacks);
    }

    public void Dispose()
    {
        List<CallbackInvocation> callbacks = new();
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            foreach (CacheEntry entry in _entries.Values)
            {
                entry.Expire(EvictionReason.Removed);
                callbacks.AddRange(entry.TakeCallbacks());
            }
            _entries.Clear();
            _size = 0;
        }
        Invoke(callbacks);
    }

    private List<CallbackInvocation> CompactLocked(int removeCount, EvictionReason reason)
    {
        var entries = new List<CacheEntry>(_entries.Values);
        entries.Sort(static (left, right) =>
        {
            int priority = left.Priority.CompareTo(right.Priority);
            return priority != 0 ? priority : left.LastAccessed.CompareTo(right.LastAccessed);
        });
        var callbacks = new List<CallbackInvocation>();
        int removed = 0;
        for (int i = 0; i < entries.Count && removed < removeCount; i++)
        {
            CacheEntry entry = entries[i];
            if (entry.Priority == CacheItemPriority.NeverRemove) continue;
            if (!_entries.Remove(entry.Key)) continue;
            _size -= entry.SizeValue;
            entry.Expire(reason);
            callbacks.AddRange(entry.TakeCallbacks());
            removed++;
        }
        return callbacks;
    }

    private List<CallbackInvocation> CompactToSizeLocked(long incomingSize, long limit)
    {
        var entries = new List<CacheEntry>(_entries.Values);
        entries.Sort(static (left, right) =>
        {
            int priority = left.Priority.CompareTo(right.Priority);
            return priority != 0 ? priority : left.LastAccessed.CompareTo(right.LastAccessed);
        });
        var callbacks = new List<CallbackInvocation>();
        foreach (CacheEntry entry in entries)
        {
            if (FitsWithinLimit(_size, incomingSize, limit)) break;
            if (entry.Priority == CacheItemPriority.NeverRemove) continue;
            if (!_entries.Remove(entry.Key)) continue;
            _size -= entry.SizeValue;
            entry.Expire(EvictionReason.Capacity);
            callbacks.AddRange(entry.TakeCallbacks());
        }
        return callbacks;
    }

    private DateTimeOffset Now => _options.EffectiveClock.UtcNow;

    private static bool FitsWithinLimit(long currentSize, long incomingSize, long limit) =>
        incomingSize <= limit - currentSize;

    private static void Invoke(List<CallbackInvocation>? callbacks)
    {
        if (callbacks is null) return;
        foreach (CallbackInvocation callback in callbacks)
        {
            try
            {
                callback.Callback(callback.Key, callback.Value, callback.Reason, callback.State);
            }
            catch (Exception)
            {
                // Match Microsoft.Extensions.Caching.Memory: one failing post-eviction
                // callback must not escape the cache operation or suppress later callbacks.
            }
        }
    }

    private void CheckDisposed()
    {
        lock (_gate) CheckDisposedLocked();
    }

    private void CheckDisposedLocked()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MemoryCache));
    }

    internal sealed class CacheEntry : ICacheEntry
    {
        private readonly MemoryCache _cache;
        private readonly List<IDisposable> _tokenRegistrations = new();
        private bool _disposed;
        private EvictionReason _reason;
        private bool _valueSet;
        private TimeSpan? _relativeExpiration;
        private TimeSpan? _slidingExpiration;
        private long? _size;

        internal CacheEntry(MemoryCache cache, object key) { _cache = cache; Key = key; }

        public object Key { get; }
        public object? Value { get => _value; set { _value = value; _valueSet = true; } }
        private object? _value;
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        public TimeSpan? AbsoluteExpirationRelativeToNow
        {
            get => _relativeExpiration;
            set
            {
                if (value <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(value));
                _relativeExpiration = value;
            }
        }
        public TimeSpan? SlidingExpiration
        {
            get => _slidingExpiration;
            set
            {
                if (value <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(value));
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
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                _size = value;
            }
        }
        internal long SizeValue => Size ?? 0;
        internal DateTimeOffset LastAccessed { get; set; }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_valueSet) _cache.Commit(this);
        }

        internal void ApplyRelativeExpiration(DateTimeOffset now)
        {
            if (AbsoluteExpirationRelativeToNow is TimeSpan relative)
            {
                DateTimeOffset computed = now + relative;
                if (AbsoluteExpiration is null || computed < AbsoluteExpiration.Value) AbsoluteExpiration = computed;
            }
        }

        internal bool IsExpired(DateTimeOffset now) => _reason != EvictionReason.None ||
            (AbsoluteExpiration is DateTimeOffset absolute && absolute <= now) ||
            (SlidingExpiration is TimeSpan sliding && LastAccessed != default && LastAccessed + sliding <= now) ||
            HasChangedToken();

        internal bool HasChangedTokenValue => HasChangedToken();

        internal void Expire(EvictionReason reason)
        {
            if (_reason == EvictionReason.None) _reason = reason;
            foreach (IDisposable registration in _tokenRegistrations) registration.Dispose();
            _tokenRegistrations.Clear();
        }

        internal void AttachTokens(MemoryCache cache)
        {
            foreach (IChangeToken token in ExpirationTokens)
            {
                if (token.ActiveChangeCallbacks)
                {
                    _tokenRegistrations.Add(token.RegisterChangeCallback(static state =>
                        ((CacheEntry)state!)._cache.TokenExpired((CacheEntry)state!), this));
                }
            }
        }

        internal List<CallbackInvocation> TakeCallbacks()
        {
            var callbacks = new List<CallbackInvocation>();
            foreach (PostEvictionCallbackRegistration registration in PostEvictionCallbacks)
            {
                if (registration.EvictionCallback is not null)
                    callbacks.Add(new CallbackInvocation(registration.EvictionCallback, Key, Value, _reason, registration.State));
            }
            PostEvictionCallbacks.Clear();
            return callbacks;
        }

        private bool HasChangedToken()
        {
            foreach (IChangeToken token in ExpirationTokens) if (token.HasChanged) return true;
            return false;
        }
    }

    internal readonly record struct CallbackInvocation(PostEvictionDelegate Callback, object Key, object? Value, EvictionReason Reason, object? State);
}
