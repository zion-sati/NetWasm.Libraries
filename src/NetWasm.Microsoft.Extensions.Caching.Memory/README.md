# NetWasm.Microsoft.Extensions.Caching.Memory

An in-process cache for `netwasm0.1` with absolute/sliding expiration,
change-token invalidation, size limits, priorities, compaction, and eviction
callbacks. Expiration is evaluated on cache operations; no guest thread or
background scheduler is created. Tests can inject `ISystemClock`.
