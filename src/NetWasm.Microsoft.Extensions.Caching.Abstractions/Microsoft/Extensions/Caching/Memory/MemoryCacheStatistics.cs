namespace Microsoft.Extensions.Caching.Memory;

public sealed class MemoryCacheStatistics
{
    public long CurrentEntryCount { get; set; }

    public long? CurrentEstimatedSize { get; set; }

    public long TotalHits { get; set; }

    public long TotalMisses { get; set; }

}
