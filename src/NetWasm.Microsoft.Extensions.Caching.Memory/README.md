# NetWasm.Microsoft.Extensions.Caching.Memory

An in-process cache for NetWasm (`net10.0-netwasm`) with absolute/sliding expiration,
change-token invalidation, size limits, priorities, compaction, and eviction
callbacks. Expiration is evaluated on cache operations; no guest thread or
background scheduler is created. Tests can inject `ISystemClock`.
