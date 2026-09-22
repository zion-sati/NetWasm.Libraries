using System;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Memory;

public class MemoryCacheOptions : IOptions<MemoryCacheOptions>
{
    private long? _sizeLimit;
    private double _compactionPercentage = 0.05;

    public ISystemClock? Clock { get; set; }

    public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(1);

    public long? SizeLimit
    {
        get => _sizeLimit;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _sizeLimit = value;
        }
    }

    public double CompactionPercentage
    {
        get => _compactionPercentage;
        set
        {
            if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value));
            _compactionPercentage = value;
        }
    }

    public bool TrackLinkedCacheEntries { get; set; }

    public bool TrackStatistics { get; set; }

    public string Name { get; set; } = "Default";

    MemoryCacheOptions IOptions<MemoryCacheOptions>.Value => this;

    internal ISystemClock EffectiveClock => Clock ??= new SystemClock();
}
