using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using TUnit.Assertions;
using TUnit.Core;

namespace NetWasm.Microsoft.Extensions.Caching.Memory.Tests;

public sealed class MemoryCacheTests
{
    [Test]
    public async Task AbsoluteAndSlidingExpirationUseInjectedClock()
    {
        var clock = new ManualClock(DateTimeOffset.UnixEpoch);
        using var cache = CreateCache(clock);
        cache.Set("absolute", "value", new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
        cache.Set("sliding", "value", new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) });

        clock.Advance(TimeSpan.FromMinutes(4));
        await Assert.That(cache.Get<string>("absolute")).IsEqualTo("value");
        await Assert.That(cache.Get<string>("sliding")).IsEqualTo("value");
        clock.Advance(TimeSpan.FromMinutes(2));
        await Assert.That(cache.Get<string>("absolute")).IsNull();
        await Assert.That(cache.Get<string>("sliding")).IsEqualTo("value");
        clock.Advance(TimeSpan.FromMinutes(6));
        await Assert.That(cache.Get<string>("sliding")).IsNull();
    }

    [Test]
    public async Task ChangeTokenEvictsAndRunsCallbackOnce()
    {
        using var source = new CancellationTokenSource();
        using var cache = CreateCache(new ManualClock(DateTimeOffset.UnixEpoch));
        var callback = new TaskCompletionSource<EvictionReason>();
        int callbackCount = 0;
        cache.Set("token", "value", new MemoryCacheEntryOptions()
            .AddExpirationToken(new CancellationChangeToken(source.Token))
            .RegisterPostEvictionCallback((_, _, evictionReason, _) =>
            {
                callbackCount++;
                callback.TrySetResult(evictionReason);
            }));

        source.Cancel();

        await Assert.That(cache.Get<string>("token")).IsNull();
        EvictionReason reason = await callback.Task;
        await Assert.That(reason).IsEqualTo(EvictionReason.TokenExpired);
        await Assert.That(callbackCount).IsEqualTo(1);
        await Assert.That(cache.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SizeLimitCompactsLowestPriorityEntries()
    {
        using var cache = new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions { SizeLimit = 2, CompactionPercentage = 1 }));
        var callback = new TaskCompletionSource<EvictionReason>();
        int callbackCount = 0;
        cache.Set("low", "low", new MemoryCacheEntryOptions { Size = 1, Priority = CacheItemPriority.Low }
            .RegisterPostEvictionCallback((_, _, evictionReason, _) =>
            {
                callbackCount++;
                callback.TrySetResult(evictionReason);
            }));
        cache.Set("high", "high", new MemoryCacheEntryOptions { Size = 1, Priority = CacheItemPriority.High });
        cache.Compact(0.5);
        cache.Set("new", "new", new MemoryCacheEntryOptions { Size = 1, Priority = CacheItemPriority.High });

        await Assert.That(cache.Get<string>("low")).IsNull();
        await Assert.That(cache.Get<string>("high")).IsEqualTo("high");
        await Assert.That(cache.Get<string>("new")).IsEqualTo("new");
        EvictionReason reason = await callback.Task;
        await Assert.That(reason).IsEqualTo(EvictionReason.Capacity);
        await Assert.That(callbackCount).IsEqualTo(1);
    }

    [Test]
    [Timeout(5000)]
    public async Task EvictionCallbackFailuresDoNotEscapeOrSuppressLaterCallbacks(CancellationToken cancellationToken)
    {
        using var cache = CreateCache(new ManualClock(DateTimeOffset.UnixEpoch));
        var laterCallback = new TaskCompletionSource<bool>();
        cache.Set("callbacks", "value", new MemoryCacheEntryOptions()
            .RegisterPostEvictionCallback((_, _, _, _) => throw new InvalidOperationException("callback failure"))
            .RegisterPostEvictionCallback((_, _, _, _) => laterCallback.TrySetResult(true)));

        bool removeThrew = false;
        try { cache.Remove("callbacks"); }
        catch (Exception) { removeThrew = true; }

        await laterCallback.Task.WaitAsync(cancellationToken);
        await Assert.That(removeThrew).IsFalse();
        await Assert.That(cache.Get<string>("callbacks")).IsNull();
    }

    [Test]
    public async Task CompactPreservesNeverRemoveAndUpdatesEntryStatistics()
    {
        using var cache = CreateCache(new ManualClock(DateTimeOffset.UnixEpoch));
        cache.Set("ordinary", "ordinary", new MemoryCacheEntryOptions { Priority = CacheItemPriority.Low });
        cache.Set("retained", "retained", new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove });

        cache.Compact(1);

        await Assert.That(cache.Get<string>("ordinary")).IsNull();
        await Assert.That(cache.Get<string>("retained")).IsEqualTo("retained");
        await Assert.That(cache.GetCurrentStatistics()!.CurrentEntryCount).IsEqualTo(1);

        cache.Clear();

        await Assert.That(cache.GetCurrentStatistics()!.CurrentEntryCount).IsEqualTo(0);
    }

    [Test]
    public async Task GetOrCreateRunsFactoryOnlyOnMiss()
    {
        using var cache = CreateCache(new ManualClock(DateTimeOffset.UnixEpoch));
        int calls = 0;
        string first = cache.GetOrCreate("key", _ => { calls++; return "created"; })!;
        string second = cache.GetOrCreate("key", _ => { calls++; return "other"; })!;

        await Assert.That(first).IsEqualTo("created");
        await Assert.That(second).IsEqualTo("created");
        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task SizeLimitRejectsUnsizedEntriesAndDistributedCacheUsesPayloadSize()
    {
        using var cache = new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions { SizeLimit = 3 }));
        bool unsizedRejected = false;
        try { cache.Set("unsized", "value"); }
        catch (InvalidOperationException) { unsizedRejected = true; }

        var distributed = new MemoryDistributedCache(
            new OptionsWrapper<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions { SizeLimit = 3 }));
        distributed.Set("fits", new byte[] { 1, 2, 3 }, new DistributedCacheEntryOptions());
        await Assert.That(unsizedRejected).IsTrue();
        await Assert.That(distributed.Get("fits")!.Length).IsEqualTo(3);
#if NETWASM
        distributed.Dispose();
#endif
    }

    [Test]
    public async Task SizeLimitAdmissionDoesNotOverflow()
    {
        using var cache = new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(
            new MemoryCacheOptions { SizeLimit = long.MaxValue }));
        cache.Set("full", "retained", new MemoryCacheEntryOptions
        {
            Size = long.MaxValue,
            Priority = CacheItemPriority.NeverRemove,
        });
        cache.Set("overflow", "rejected", new MemoryCacheEntryOptions { Size = 1 });

        await Assert.That(cache.Get<string>("full")).IsEqualTo("retained");
        await Assert.That(cache.Get<string>("overflow")).IsNull();
    }

    private static MemoryCache CreateCache(ManualClock clock) =>
        new(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions { Clock = clock, TrackStatistics = true }));

    private sealed class ManualClock : ISystemClock
    {
        public ManualClock(DateTimeOffset now) => UtcNow = now;

        public DateTimeOffset UtcNow { get; private set; }

        public void Advance(TimeSpan amount) => UtcNow += amount;
    }
}
