namespace Microsoft.Extensions.Caching.Memory;

public enum EvictionReason
{
    None,
    Removed,
    Replaced,
    Expired,
    TokenExpired,
    Capacity,
}
